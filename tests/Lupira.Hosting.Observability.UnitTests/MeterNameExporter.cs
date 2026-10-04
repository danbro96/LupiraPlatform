using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Lupira.Hosting.Observability.UnitTests;

internal sealed class MeterNameExporter : BaseExporter<Metric>
{
    public HashSet<string> MeterNames { get; } = [];

    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (var metric in batch)
            MeterNames.Add(metric.MeterName);
        return ExportResult.Success;
    }
}
