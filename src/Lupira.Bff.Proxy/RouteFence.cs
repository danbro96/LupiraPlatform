namespace Lupira.Bff.Proxy;

public sealed class RouteFence
{
    public required string Cluster { get; set; }

    public required string Verb { get; set; }

    public required string Path { get; set; }

    public required string UpstreamEntry { get; set; }
}
