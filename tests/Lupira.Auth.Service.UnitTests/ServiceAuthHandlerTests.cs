using System.Net;
using Lupira.Auth.Service.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Lupira.Auth.Service.UnitTests;

public sealed class ServiceAuthHandlerTests
{
    private static async Task<WebApplication> StartAsync(string environment, ServiceAuthOptions? options = null)
    {
        options ??= new ServiceAuthOptions();
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication().AddLupiraServiceAuth(options);
        builder.Services.AddAuthorization(o => o.AddLupiraServicePolicy(options));
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/ingest", (CurrentService service) => service.RequireCaller().Actor).RequireAuthorization(ServiceAuthHandler.SchemeName);
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> GetAsync(WebApplication app, string? serviceId)
    {
        var client = app.GetTestClient();
        if (serviceId is not null) client.DefaultRequestHeaders.TryAddWithoutValidation(ServiceAuthHandler.DevHeaderName, serviceId);
        return await client.GetAsync("/ingest");
    }

    [Fact]
    public async Task The_dev_header_authenticates_a_service_in_Development()
    {
        await using var app = await StartAsync(Environments.Development);

        var res = await GetAsync(app, "cal-worker");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("service:cal-worker", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_dev_header_is_ignored_outside_Development()
    {
        await using var app = await StartAsync(Environments.Production);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(app, "cal-worker")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task A_missing_or_empty_header_is_401(string? serviceId)
    {
        await using var app = await StartAsync(Environments.Development);

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetAsync(app, serviceId)).StatusCode);
    }

    [Fact]
    public async Task The_bearer_scheme_is_registered_only_when_configured()
    {
        await using var bare = await StartAsync(Environments.Development);
        await using var configured = await StartAsync(Environments.Development, new ServiceAuthOptions { Authority = "https://auth.example", Audience = "svc" });

        Assert.Null(await bare.Services.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(ServiceAuthOptions.JwtSchemeName));
        Assert.NotNull(await configured.Services.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(ServiceAuthOptions.JwtSchemeName));
    }

    [Fact]
    public void The_policy_schemes_include_the_bearer_only_when_configured()
    {
        Assert.Equal([ServiceAuthHandler.SchemeName], new ServiceAuthOptions().AuthenticationSchemes);
        Assert.Equal(
            [ServiceAuthHandler.SchemeName, ServiceAuthOptions.JwtSchemeName],
            new ServiceAuthOptions { Authority = "https://auth.example", Audience = "svc" }.AuthenticationSchemes);
    }
}
