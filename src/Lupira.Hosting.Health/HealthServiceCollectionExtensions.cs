using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lupira.Hosting.Health;

public static class HealthServiceCollectionExtensions
{
    public static readonly TimeSpan DefaultReadyTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Registers the <c>self</c> liveness check; chain <see cref="AddReadyCheck{T}"/> for readiness.</summary>
    public static IHealthChecksBuilder AddLupiraHealth(this IServiceCollection services) =>
        services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: [HealthTags.Live]);

    /// <summary>A <see cref="HealthTags.Ready"/> check with a hard timeout, so a hung dependency fails the probe fast.</summary>
    public static IHealthChecksBuilder AddReadyCheck<T>(this IHealthChecksBuilder builder, string name, TimeSpan? timeout = null)
        where T : class, IHealthCheck =>
        builder.AddCheck<T>(name, HealthStatus.Unhealthy, [HealthTags.Ready], timeout ?? DefaultReadyTimeout);
}
