namespace Lupira.Bff.Proxy;

public sealed class ExposedOperation
{
    public required ExposedGroup Group { get; set; }

    public required string Cluster { get; set; }

    public required string Verb { get; set; }

    public required string UpstreamPath { get; set; }

    public required string MappedPath { get; set; }

    public required string BffPath { get; set; }

    public string UpstreamEntry => $"{Verb} {UpstreamPath}";
}
