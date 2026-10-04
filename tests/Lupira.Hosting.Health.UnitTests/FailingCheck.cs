using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lupira.Hosting.Health.UnitTests;

internal sealed class FailingCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Unhealthy("database down"));
}
