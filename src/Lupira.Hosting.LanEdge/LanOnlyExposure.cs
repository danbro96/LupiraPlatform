using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Lupira.Hosting.LanEdge;

/// <summary>
/// Defence-in-depth backstop keeping the given prefixes LAN/WireGuard-only. The PRIMARY control is the edge
/// (Cloudflare Tunnel ingress, DMZ reverse proxy) not routing them to the container (host config, outside this
/// repo). This middleware survives an edge mistake: a request through any edge carries <c>X-Forwarded-For</c>
/// (Cloudflare also adds <c>CF-Ray</c>/<c>CF-Connecting-IP</c>), which a direct LAN/WireGuard request never does —
/// so an edge hit is answered 404, indistinguishable from "no such route". Plain middleware rather than an
/// endpoint filter because <c>MapMcp</c>'s streaming endpoint does not run the minimal-API filter pipeline.
/// </summary>
public static class LanOnlyExposure
{
    private static readonly string[] EdgeHeaders = ["CF-Ray", "CF-Connecting-IP", "X-Forwarded-For"];

    public static IApplicationBuilder UseLanOnlySurfaces(this IApplicationBuilder app, params string[] lanOnlyPrefixes)
    {
        return app.Use(async (ctx, next) =>
        {
            if (lanOnlyPrefixes.Any(p => ctx.Request.Path.StartsWithSegments(p))
                && EdgeHeaders.Any(h => ctx.Request.Headers.ContainsKey(h)))
            {
                // Came in through an edge proxy — pretend the route doesn't exist.
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(ctx);
        });
    }
}
