namespace Lupira.Bff.Proxy;

/// <summary>Where an upstream is mounted on the BFF.</summary>
public sealed class ExposedCluster
{
    public required string Prefix { get; set; }

    /// <summary>
    /// Sends the mount as <c>X-Forwarded-Prefix</c>, for an upstream that builds callback URLs (and cookie
    /// paths) from its PathBase that must route back through the BFF.
    /// </summary>
    public bool AnnouncesPrefix { get; set; }
}
