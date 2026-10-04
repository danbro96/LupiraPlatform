namespace Lupira.Hosting.Observability;

public sealed class LupiraTelemetryOptions
{
    /// <summary>Defaults to the entry assembly's version.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>Meters beyond the always-registered <c>Lupira.*</c> and <c>&lt;ApplicationName&gt;.*</c>.</summary>
    public IList<string> Meters { get; } = [];

    /// <summary>Activity sources beyond the always-registered <c>Lupira.*</c> and <c>&lt;ApplicationName&gt;.*</c>.</summary>
    public IList<string> Sources { get; } = [];

    /// <summary>Request path prefixes whose server spans are dropped; probes are listed by default.</summary>
    public IList<string> FilteredPaths { get; } = ["/livez", "/readyz", "/pingz", "/depz"];

    /// <summary>Exports to the console in Development when no OTLP endpoint is set.</summary>
    public bool ConsoleInDevelopment { get; set; }
}
