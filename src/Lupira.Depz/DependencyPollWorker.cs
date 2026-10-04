using Lupira.Contracts.Depz;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Depz;

/// <summary>Sweeps every outward edge on a fixed interval; a bad sweep never kills the loop.</summary>
internal sealed class DependencyPollWorker(
    DependencyProbe probe, IDependencyTargetSource targets, DependencyReportCache cache,
    IOptions<DepzOptions> opts, ILogger<DependencyPollWorker> logger) : BackgroundService
{
    private readonly DepzOptions _opts = opts.Value;

    internal async Task SweepSafelyAsync(CancellationToken ct)
    {
        try
        {
            var results = await Task.WhenAll(targets.GetTargets().Select(t => probe.ProbeAsync(t, ct)));
            cache.Set(new DepzReportDto
            {
                Service = _opts.ServiceName,
                LastPolledUtc = DateTimeOffset.UtcNow,
                Dependencies = results,
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dependency sweep failed; next tick retries.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opts.Enabled)
            return;

        try
        {
            await Task.Delay(_opts.StartupDelay, stoppingToken);
            await SweepSafelyAsync(stoppingToken);
            using var timer = new PeriodicTimer(_opts.PollInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await SweepSafelyAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
