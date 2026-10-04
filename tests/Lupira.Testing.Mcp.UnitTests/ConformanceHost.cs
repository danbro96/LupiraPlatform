using Lupira.Auth.DevUser;
using Lupira.Hosting.LanEdge;
using Lupira.Mcp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Testing.Mcp.UnitTests;

/// <summary>An in-memory host wired like a Lupira MCP API, for running the shared cases against.</summary>
public sealed class ConformanceHost : IAsyncLifetime
{
    public const string Issuer = "https://auth.test/application/o/lupira-conformance/";

    private WebApplication? _app;

    public HttpClient CreateClient() => _app!.GetTestClient();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = DevAuthenticationBuilderExtensions.DefaultScheme;
                o.DefaultChallengeScheme = McpChallengeHandler.Scheme;
            })
            .AddLupiraDevHeaderAuth()
            .AddScheme<AuthenticationSchemeOptions, McpChallengeHandler>(McpChallengeHandler.Scheme, null);
        builder.Services.AddAuthorizationBuilder().AddPolicy("ApiPolicy", p => p.RequireAuthenticatedUser());
        builder.Services.AddLupiraMcp().WithTools<ConformanceTools>();

        _app = builder.Build();
        _app.UseLanOnlySurfaces("/mcp", "/.well-known/oauth-protected-resource");
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapMcpResourceMetadata(Issuer);
        _app.MapLupiraMcp();
        _app.MapGet("/me", () => "me").RequireAuthorization("ApiPolicy");
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null) await _app.DisposeAsync();
    }
}
