using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lupira.Auth.Jwt.UnitTests;

public sealed class GuardTests
{
    [Theory]
    [InlineData(OidcConfigRequirement.OutsideDevelopment, "Production", null, null, "Auth:Oidc Authority + Audience are required outside Development.")]
    [InlineData(OidcConfigRequirement.OutsideDevelopment, "Production", "https://a.test/", null, "Auth:Oidc Authority + Audience are required outside Development.")]
    [InlineData(OidcConfigRequirement.OutsideDevelopment, "Development", null, null, null)]
    [InlineData(OidcConfigRequirement.Always, "Development", null, "aud", "Auth:Oidc:Authority is required.")]
    [InlineData(OidcConfigRequirement.Always, "Production", "https://a.test/", " ", "Auth:Oidc:Audience is required.")]
    [InlineData(OidcConfigRequirement.Always, "Production", "https://a.test/", "aud", null)]
    [InlineData(OidcConfigRequirement.None, "Production", null, null, null)]
    public void Missing_config_fails_startup_per_requirement(
        OidcConfigRequirement requirement, string environment, string? authority, string? audience, string? expected)
    {
        var builder = Builder(environment, authority, audience);

        var error = Record.Exception(() => builder.AddLupiraJwt(o => o.RequireConfig = requirement, []));

        Assert.Equal(expected, error?.Message);
    }

    [Theory]
    [InlineData(OidcConfigRequirement.OutsideDevelopment)]
    [InlineData(OidcConfigRequirement.Always)]
    public void Build_time_openapi_generation_skips_the_guard(OidcConfigRequirement requirement)
    {
        var builder = Builder("Production", null, null);

        var error = Record.Exception(() => builder.AddLupiraJwt(o => o.RequireConfig = requirement, ["/x/dotnet-GetDocument.Insider.dll"]));

        Assert.Null(error);
    }

    [Fact]
    public void The_message_names_a_custom_section()
    {
        var builder = Builder("Production", null, null);

        var error = Assert.Throws<InvalidOperationException>(() => builder.AddLupiraJwt(o => o.SectionName = "Auth", []));

        Assert.Equal("Auth Authority + Audience are required outside Development.", error.Message);
    }

    [Fact]
    public void DevOrJwt_without_a_dev_scheme_is_rejected_in_Development()
    {
        var builder = Builder("Development", null, null);

        Assert.Throws<InvalidOperationException>(() => builder.AddLupiraJwt(
            o =>
            {
                o.DevOrJwtDefault = true;
                o.DevScheme = null;
            },
            []));
    }

    [Theory]
    [InlineData(new[] { "dotnet", "exec", "GetDocument.Insider.dll" }, true)]
    [InlineData(new[] { "/app/LupiraCalApi.dll" }, false)]
    public void OpenApiBuild_detects_getdocument(string[] args, bool expected) =>
        Assert.Equal(expected, OpenApiBuild.IsRunning(args));

    private static WebApplicationBuilder Builder(string environment, string? authority, string? audience)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Oidc:Authority"] = authority,
            ["Auth:Oidc:Audience"] = audience,
        });
        return builder;
    }
}
