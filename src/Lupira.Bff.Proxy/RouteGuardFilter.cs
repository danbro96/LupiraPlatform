using Yarp.ReverseProxy.Configuration;

namespace Lupira.Bff.Proxy;

/// <summary>
/// Applies the type constraints when YARP loads the routes: the specs arrive through DI, after the
/// route configuration source has already been added.
/// </summary>
internal sealed class RouteGuardFilter(RouteGuardPlan plan) : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel) =>
        ValueTask.FromResult(cluster);

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel) =>
        ValueTask.FromResult(plan.Paths.TryGetValue(route.RouteId, out var path)
            ? route with { Match = route.Match with { Path = path } }
            : route);
}
