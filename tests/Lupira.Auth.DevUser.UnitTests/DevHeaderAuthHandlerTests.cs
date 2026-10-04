using Xunit;

namespace Lupira.Auth.DevUser.UnitTests;

public sealed class DevHeaderAuthHandlerTests
{
    private static Task<Microsoft.AspNetCore.Builder.WebApplication> StartAsync(string scheme = DevAuthenticationBuilderExtensions.DefaultScheme) =>
        DevAuthTestHost.StartAsync(a => a.AddLupiraDevHeaderAuth(scheme), scheme: scheme);

    [Fact]
    public async Task A_missing_header_is_no_result()
    {
        await using var app = await StartAsync();

        var who = await app.WhoAsync();

        Assert.Equal("none", who.Outcome);
    }

    [Fact]
    public async Task An_empty_header_fails()
    {
        await using var app = await StartAsync();

        var who = await app.WhoAsync(("X-Dev-User", "  "));

        Assert.Equal("fail", who.Outcome);
    }

    [Fact]
    public async Task The_header_becomes_a_bearer_shaped_principal()
    {
        await using var app = await StartAsync();

        var who = await app.WhoAsync(("X-Dev-User", " Alice@Example.SE "));

        Assert.Equal("success", who.Outcome);
        Assert.Equal("alice@example.se", who.Name);
        Assert.Equal("Dev", who.AuthenticationType);
        Assert.Equal(["sub=dev|alice@example.se", "email=alice@example.se"], who.Claims);
    }

    [Fact]
    public async Task Groups_are_comma_separated_roles()
    {
        await using var app = await StartAsync();

        var who = await app.WhoAsync(("X-Dev-User", "a@b.se"), ("X-Dev-Groups", "tasks-admins, family,,"));

        Assert.Equal(["tasks-admins", "family"], who.Groups);
        Assert.Contains("groups=family", who.Claims);
    }

    [Fact]
    public async Task Scopes_and_client_become_scope_and_azp()
    {
        await using var app = await StartAsync();

        var who = await app.WhoAsync(("X-Dev-User", "a@b.se"), ("X-Dev-Scopes", "internal:read openid"), ("X-Dev-Client", "dav-gateway"));

        Assert.Contains("scope=internal:read openid", who.Claims);
        Assert.Contains("azp=dav-gateway", who.Claims);
    }

    [Fact]
    public async Task The_identity_carries_the_registered_scheme_name()
    {
        await using var app = await StartAsync("DevHeader");

        var who = await app.WhoAsync(("X-Dev-User", "a@b.se"));

        Assert.Equal("DevHeader", who.AuthenticationType);
    }
}
