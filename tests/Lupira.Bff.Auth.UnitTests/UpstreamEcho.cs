namespace Lupira.Bff.Auth.UnitTests;

public sealed class UpstreamEcho
{
    public required string Path { get; set; }

    public required string Authorization { get; set; }

    public required string XDevUser { get; set; }

    public required string XForwardedPrefix { get; set; }
}
