namespace Lupira.Bff.Proxy;

public sealed class RouteGuardPlan
{
    /// <summary>Route key → its template with parameter type constraints; routes without any are absent.</summary>
    public required IReadOnlyDictionary<string, string> Paths { get; set; }

    /// <summary>Unlisted upstream operations a listed template would otherwise forward.</summary>
    public required IReadOnlyList<RouteFence> Fences { get; set; }

    public required IReadOnlyList<string> UnguardedClusters { get; set; }
}
