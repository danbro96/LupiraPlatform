namespace Lupira.Depz;

/// <summary>Binds <c>Depz</c> — the non-gating dependency probe (/depz). Blank
/// <see cref="ProbeKey"/> = feature off.</summary>
public sealed class DepzOptions
{
    public const string SectionName = "Depz";

    public string ProbeKey { get; set; } = string.Empty;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public string ServiceName { get; set; } = string.Empty;

    public string MeterName { get; set; } = "Lupira.Depz";

    public string MetricPrefix { get; set; } = string.Empty;

    public bool Enabled => !string.IsNullOrWhiteSpace(ProbeKey);
}
