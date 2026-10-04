namespace Lupira.Bff.Proxy;

public sealed class ProxyRoute
{
    public required string Key { get; set; }

    public required ExposedGroup Group { get; set; }

    public required string Cluster { get; set; }

    public required string Path { get; set; }

    public required IReadOnlyList<string> Verbs { get; set; }

    public string? RemovePrefix { get; set; }

    public bool AnnouncesPrefix { get; set; }
}
