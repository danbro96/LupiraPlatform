using Lupira.Contracts.Depz;
using Microsoft.Extensions.Options;

namespace Lupira.Depz;

/// <summary>Last completed sweep, atomically swapped so /depz serves from memory.</summary>
public sealed class DependencyReportCache(IOptions<DepzOptions> options)
{
    private volatile DepzReportDto _report = new()
    {
        Service = options.Value.ServiceName,
        LastPolledUtc = null,
        Dependencies = [],
    };

    public DepzReportDto Current() => _report;

    public void Set(DepzReportDto report) => _report = report;
}
