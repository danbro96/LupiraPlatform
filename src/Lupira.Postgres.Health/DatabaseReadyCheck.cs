using Marten;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lupira.Postgres.Health;

/// <summary>Readiness: a cheap <c>select 1</c> proving the Marten store's Postgres is reachable and its credentials
/// work. The exception rides on the result so a failure stays diagnosable.</summary>
public sealed class DatabaseReadyCheck(IDocumentStore store) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = store.QuerySession();
            await session.QueryAsync<int>("select 1", cancellationToken);
            return HealthCheckResult.Healthy("Postgres reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres unreachable.", ex);
        }
    }
}
