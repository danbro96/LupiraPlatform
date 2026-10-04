using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lupira.Depz;

public static class DepzServiceCollectionExtensions
{
    /// <summary>The poller only runs when <see cref="DepzOptions.ProbeKey"/> is set; targets come from an
    /// <see cref="IDependencyTargetSource"/> the app registers.</summary>
    public static IServiceCollection AddLupiraDepz(this IServiceCollection services, Action<DepzOptions> configure)
    {
        services.AddOptions<DepzOptions>()
            .Configure(configure)
            .Validate(o => !string.IsNullOrWhiteSpace(o.ServiceName), "Depz ServiceName is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.MetricPrefix), "Depz MetricPrefix is required.");
        services.AddMetrics();
        services.AddSingleton<DependencyReportCache>();
        services.AddSingleton<DependencyTelemetry>();
        services.AddSingleton<DependencyProbe>();
        services.AddHttpClient(DependencyProbe.ProbeClientName)
            .ConfigureHttpClient((sp, c) => c.Timeout = sp.GetRequiredService<IOptions<DepzOptions>>().Value.ProbeTimeout);
        services.AddHostedService<DependencyPollWorker>();
        return services;
    }

    public static IServiceCollection AddLupiraDepzTargets(this IServiceCollection services, IReadOnlyList<DependencyTarget> targets) =>
        services.AddSingleton<IDependencyTargetSource>(new StaticDependencyTargetSource(targets));

    public static IServiceCollection AddLupiraDepzConfigurationTargets(this IServiceCollection services, params ConfiguredTarget[] targets) =>
        services.AddSingleton<IDependencyTargetSource>(sp =>
            new ConfigurationDependencyTargetSource(sp.GetRequiredService<IConfiguration>(), targets));
}
