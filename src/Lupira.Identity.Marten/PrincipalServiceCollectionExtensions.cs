using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Identity.Marten;

public static class PrincipalServiceCollectionExtensions
{
    public static IServiceCollection AddLupiraPrincipalDirectory(this IServiceCollection services) =>
        services.AddScoped<PrincipalDirectory>();

    public static IServiceCollection AddLupiraPrincipalDirectory<TPrincipal>(this IServiceCollection services)
        where TPrincipal : Principal, new() =>
        services.AddScoped<PrincipalDirectory<TPrincipal>>();
}
