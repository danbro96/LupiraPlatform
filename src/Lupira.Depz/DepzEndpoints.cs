using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lupira.Depz;

/// <summary>Non-gating dependency report: this service's outward edges, served from the
/// poller's cache. Deliberately not part of /readyz.</summary>
public static class DepzEndpoints
{
    public static RouteHandlerBuilder MapDepz(this IEndpointRouteBuilder app) =>
        app.MapGet("/depz", (DependencyReportCache cache) => TypedResults.Ok(cache.Current()))
            .AllowAnonymous()
            .AddEndpointFilter<ProbeKeyFilter>()
            .ExcludeFromDescription()
            .DisableHttpMetrics()
            .WithName("GetDependencies");
}
