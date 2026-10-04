using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lupira.Bff.Proxy.UnitTests;

public sealed class ProxyHost : IAsyncDisposable
{
    public const string DevUserName = "dev@test";

    private readonly WebApplication _app;

    private ProxyHost(WebApplication app, StubUpstream upstream)
    {
        _app = app;
        Upstream = upstream;
    }

    public StubUpstream Upstream { get; }

    public static async Task<ProxyHost> StartAsync(
        string fixture,
        string environment = "Production",
        IReadOnlyDictionary<string, JsonObject>? specs = null,
        ILoggerProvider? logs = null)
    {
        var upstream = await StubUpstream.StartAsync();
        var surface = Fixture.Surface(fixture);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        if (logs is not null) builder.Logging.AddProvider(logs);
        builder.Configuration[DevUser.ConfigurationKey] = DevUserName;
        foreach (var cluster in surface.Operations.Select(o => o.Cluster).Distinct())
            builder.Configuration[$"ReverseProxy:Clusters:{cluster}:Destinations:primary:Address"] = upstream.Address;

        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Guest", p => p.RequireAuthenticatedUser().RequireClaim("share-token"));
        builder.AddLupiraBffProxy(o => o.Surface = surface);
        builder.Services.Configure<RouteGuardOptions>(o =>
        {
            foreach (var (cluster, spec) in specs ?? new Dictionary<string, JsonObject>())
                o.Specs[cluster] = spec;
        });

        var app = builder.Build();
        app.UseLupiraBffDeviceKeyGate();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLupiraBffProxy();
        app.MapFallback(() => Results.Content("<html></html>", "text/html"));
        await app.StartAsync();
        return new ProxyHost(app, upstream);
    }

    public HttpClient Client(string? bearer = null)
    {
        var client = _app.GetTestClient();
        if (bearer is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await Upstream.DisposeAsync();
    }
}
