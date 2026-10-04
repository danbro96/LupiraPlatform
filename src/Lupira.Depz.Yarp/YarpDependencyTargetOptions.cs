namespace Lupira.Depz.Yarp;

public sealed class YarpDependencyTargetOptions
{
    public string ClustersSection { get; set; } = "ReverseProxy:Clusters";

    public string ProbePath { get; set; } = "readyz";

    /// <summary>Cluster id → registry service name; every cluster must have one.</summary>
    public Dictionary<string, string> ServiceNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
