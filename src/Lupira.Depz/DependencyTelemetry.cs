using System.Diagnostics.Metrics;
using Lupira.Contracts.Depz;
using Microsoft.Extensions.Options;

namespace Lupira.Depz;

internal sealed class DependencyTelemetry
{
    private readonly Histogram<double> _probeDuration;

    public DependencyTelemetry(IMeterFactory meters, IOptions<DepzOptions> options, DependencyReportCache cache)
    {
        var opts = options.Value;
        var meter = meters.Create(opts.MeterName);
        _probeDuration = meter.CreateHistogram<double>($"{opts.MetricPrefix}.dependency.probe.duration", unit: "s");
        meter.CreateObservableGauge($"{opts.MetricPrefix}.dependency.up", () =>
            cache.Current().Dependencies.Select(d => new Measurement<int>(
                d.Status == DependencyStatus.Healthy ? 1 : 0,
                new KeyValuePair<string, object?>("dependency", d.Name),
                new KeyValuePair<string, object?>("status", d.Status.ToString()))));
    }

    public void Record(string dependency, DependencyStatus status, double? latencyMs)
    {
        if (latencyMs is { } ms)
        {
            _probeDuration.Record(ms / 1000d, new KeyValuePair<string, object?>("dependency", dependency),
                new KeyValuePair<string, object?>("status", status.ToString()));
        }
    }
}
