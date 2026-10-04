using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Depz.Yarp;

public static class YarpDepzServiceCollectionExtensions
{
    public static IServiceCollection AddLupiraDepzYarpTargets(
        this IServiceCollection services, Action<YarpDependencyTargetOptions> configure)
    {
        services.Configure(configure);
        return services.AddSingleton<IDependencyTargetSource, YarpDependencyTargetSource>();
    }
}
