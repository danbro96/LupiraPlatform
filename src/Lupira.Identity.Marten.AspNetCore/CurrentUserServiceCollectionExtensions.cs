using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Identity.Marten.AspNetCore;

public static class CurrentUserServiceCollectionExtensions
{
    public static IServiceCollection AddLupiraCurrentUser(this IServiceCollection services, Action<CurrentUserOptions>? configure = null)
    {
        AddOptions(services, configure);
        return services.AddScoped<CurrentUser>();
    }

    public static IServiceCollection AddLupiraCurrentUser<TPrincipal>(this IServiceCollection services, Action<CurrentUserOptions>? configure = null)
        where TPrincipal : Principal, new()
    {
        AddOptions(services, configure);
        return services.AddScoped<CurrentUser<TPrincipal>>();
    }

    private static void AddOptions(IServiceCollection services, Action<CurrentUserOptions>? configure)
    {
        services.AddHttpContextAccessor();
        var options = services.AddOptions<CurrentUserOptions>();
        if (configure is not null) options.Configure(configure);
    }
}
