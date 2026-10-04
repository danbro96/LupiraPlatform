using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Bff.Proxy;

public static class LupiraBffProxyEndpointExtensions
{
    public static IEndpointRouteBuilder MapLupiraBffProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapReverseProxy();
        return endpoints.MapApiPrefixFence();
    }

    /// <summary>
    /// A proxied prefix that matched no route is a 404, not the SPA shell. Without this the fallback
    /// answers 200/text-html for anything under an API prefix, so a removed route and a live one look
    /// alike from outside. Literal segments outrank this catch-all, so it only fires on a real miss.
    /// </summary>
    public static IEndpointRouteBuilder MapApiPrefixFence(this IEndpointRouteBuilder endpoints)
    {
        foreach (var prefix in endpoints.ServiceProvider.GetRequiredService<ExposedSurface>().ApiPrefixes)
            endpoints.Map($"{prefix}/{{**rest}}", () => TypedResults.NotFound());
        return endpoints;
    }

    /// <summary>
    /// Device-key routes are Anonymous because only the upstream holds the keys. Reject a missing or
    /// malformed header here so the route is not a blank relay, and answer 401 rather than letting a
    /// cookie handler challenge a device with an OIDC redirect. Runs before <c>UseAuthentication</c>.
    /// </summary>
    public static IApplicationBuilder UseLupiraBffDeviceKeyGate(this IApplicationBuilder app)
    {
        var prefixes = app.ApplicationServices.GetRequiredService<ExposedSurface>().DeviceKeyPrefixes;
        if (prefixes.Count == 0) return app;

        return app.UseWhen(
            ctx => prefixes.Any(p => ctx.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)),
            branch => branch.Use(async (ctx, next) =>
            {
                if (!DeviceKeyHeader.IsWellFormed(ctx.Request.Headers.Authorization))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                await next();
            }));
    }
}
