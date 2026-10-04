using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Lupira.Hosting.Health;

public static class PingEndpoints
{
    /// <summary>Authenticated <c>/pingz</c>; no policy names = the default policy.</summary>
    public static RouteHandlerBuilder MapLupiraPing(this IEndpointRouteBuilder app, params string[] policyNames) =>
        // Claims echo only — deliberately no user resolution, so a probe never provisions a principal
        // or bumps LastSeenAt. Consumers poll this from /depz to verify the auth seam.
        app.MapGet("/pingz", Ping)
            .RequireAuthorization(policyNames)
            .WithTags("Ping")
            .WithName("Ping")
            .WithSummary("Authenticated claims echo for dependency probes; resolves nothing, writes nothing.")
            .DisableHttpMetrics();

    internal static Ok<PingDto> Ping(ClaimsPrincipal user) => TypedResults.Ok(new PingDto
    {
        Subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        Audiences = user.FindAll("aud").Select(c => c.Value).ToArray(),
        Email = user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email),
    });
}
