using System.Text.Json.Nodes;

namespace Lupira.Bff.OpenApi;

public sealed class MergeResult
{
    public required JsonObject Document { get; set; }

    /// <summary>Upstream operations the allowlist omits — growth nobody has reviewed yet.</summary>
    public required IReadOnlyList<string> NotExposed { get; set; }

    /// <summary>Schemas and operation ids namespaced because two upstreams disagreed.</summary>
    public required IReadOnlyList<string> Renames { get; set; }
}
