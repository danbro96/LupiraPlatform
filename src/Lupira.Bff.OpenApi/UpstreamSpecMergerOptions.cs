using System.Text.Json.Nodes;
using Lupira.Bff.Proxy;

namespace Lupira.Bff.OpenApi;

public sealed class UpstreamSpecMergerOptions
{
    public string Title { get; set; } = "BFF";

    public string? Version { get; set; }

    /// <summary>One tag per cluster, so the generator's tag split lands each upstream in its own folder.</summary>
    public bool RetagByCluster { get; set; }

    /// <summary>A name two upstreams claim with different shapes gets its cluster as a namespace; off, it throws.</summary>
    public bool NamespaceCollisions { get; set; }

    public bool SortPaths { get; set; }

    /// <summary>The order decides which upstream keeps an unprefixed schema name.</summary>
    public IList<UpstreamSpec> Upstreams { get; } = [];

    /// <summary>The credentials presented to the BFF, not the upstreams' own.</summary>
    public IDictionary<string, JsonObject> SecuritySchemes { get; } = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

    /// <summary>Scheme names an operation accepts, as alternatives rather than all required.</summary>
    public Func<ExposedOperation, IReadOnlyList<string>> SecurityFor { get; set; } = _ => ["Cookie", "Bearer"];
}
