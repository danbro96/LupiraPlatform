using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Lupira.Depz.Yarp;

/// <summary>Roster derived from the same <c>ReverseProxy:Clusters</c> the proxy binds — edges cannot drift.
/// Availability-only: the upstreams take the signed-in user's token, which cannot be probed without a user.</summary>
public sealed class YarpDependencyTargetSource(IConfiguration configuration, IOptions<YarpDependencyTargetOptions> options)
    : IDependencyTargetSource
{
    private readonly IReadOnlyList<DependencyTarget> _targets = From(configuration, options.Value);

    public static IReadOnlyList<DependencyTarget> From(IConfiguration configuration, YarpDependencyTargetOptions options)
    {
        var names = new Dictionary<string, string>(options.ServiceNames, StringComparer.OrdinalIgnoreCase);
        return configuration.GetSection(options.ClustersSection).GetChildren()
            .Select(cluster => new DependencyTarget
            {
                Name = names.TryGetValue(cluster.Key, out var name)
                    ? name
                    : throw new InvalidOperationException($"Cluster '{cluster.Key}' has no registry service name for /depz."),
                BaseUrl = cluster.GetSection("Destinations").GetChildren()
                    .Select(destination => destination["Address"])
                    .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)) ?? string.Empty,
                ProbePath = options.ProbePath,
            })
            .ToList();
    }

    public IReadOnlyList<DependencyTarget> GetTargets() => _targets;
}
