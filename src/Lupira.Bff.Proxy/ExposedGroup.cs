namespace Lupira.Bff.Proxy;

/// <summary>Each group is a routing class: one policy, one credential.</summary>
public sealed class ExposedGroup
{
    public const string DefaultName = "operations";

    public required string Name { get; set; }

    public string Policy { get; set; } = "Default";

    /// <summary>False mounts entries at the upstream's own path: no prefix and no prefix strip.</summary>
    public bool Prefixed { get; set; } = true;

    /// <summary>
    /// File subtrees an upstream serves outside OpenAPI. These keep a catch-all because the paths (glyph
    /// ranges, sprites, tiles) cannot be enumerated — hence the verbs stay pinned to GET.
    /// </summary>
    public bool CatchAll { get; set; }

    public bool Documented { get; set; }

    public UpstreamCredential Credential { get; set; } = UpstreamCredential.Session;

    public ExposedPathMap? PathMap { get; set; }
}
