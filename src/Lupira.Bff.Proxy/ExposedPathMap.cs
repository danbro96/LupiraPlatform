namespace Lupira.Bff.Proxy;

public sealed class ExposedPathMap
{
    public required string Upstream { get; set; }

    public required string Bff { get; set; }

    /// <summary>Route parameter of <see cref="Upstream"/> → the claim type its value is replayed from.</summary>
    public required IReadOnlyDictionary<string, string> Claims { get; set; }

    public bool Covers(string upstreamPath) =>
        upstreamPath == Upstream || upstreamPath.StartsWith(Upstream + "/", StringComparison.Ordinal);

    public string ToBff(string upstreamPath) => Bff + upstreamPath[Upstream.Length..];
}
