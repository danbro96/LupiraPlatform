using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lupira.Bff.Proxy.UnitTests;

/// <summary>
/// In-process upstream the YARP clusters point at: a real Kestrel listener on an ephemeral port that echoes
/// back what the proxy actually sent, and counts hits so a test can prove a request never reached it.
/// </summary>
public sealed class StubUpstream : IAsyncDisposable
{
    private readonly WebApplication _app;
    private int _hits;

    private StubUpstream(WebApplication app) => _app = app;

    public string Address { get; private set; } = string.Empty;

    public int Hits => Volatile.Read(ref _hits);

    public static async Task<StubUpstream> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        var self = new StubUpstream(app);

        app.Map("/{**path}", (HttpContext ctx) =>
        {
            Interlocked.Increment(ref self._hits);
            return Results.Json(new UpstreamEcho
            {
                Path = ctx.Request.Path.ToString(),
                Authorization = ctx.Request.Headers.Authorization.ToString(),
                XDevUser = ctx.Request.Headers[DevUser.HeaderName].ToString(),
                XForwardedPrefix = ctx.Request.Headers["X-Forwarded-Prefix"].ToString(),
            });
        });

        await app.StartAsync();
        self.Address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return self;
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
