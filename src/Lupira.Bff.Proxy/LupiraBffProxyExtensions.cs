using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lupira.Bff.Proxy;

public static class LupiraBffProxyExtensions
{
    public static IReverseProxyBuilder AddLupiraBffProxy(
        this WebApplicationBuilder builder, Action<LupiraBffProxyOptions>? configure = null)
    {
        var options = new LupiraBffProxyOptions();
        configure?.Invoke(options);
        var surface = options.Surface ?? ExposedSurface.Load(builder.Environment);
        var routes = ProxyRoutes.Plan(surface).ToDictionary(r => r.Key, StringComparer.Ordinal);

        // One exact template per allowlisted path, as a config source: YARP's LoadFromConfig then reads
        // it exactly as it reads appsettings, and clusters stay hand-maintained.
        builder.Configuration.AddInMemoryCollection(ProxyRoutes.Build(surface));

        builder.Services.AddSingleton(surface);
        builder.Services.AddOptions<RouteGuardOptions>();
        builder.Services.AddSingleton(services => GuardPlan(services, surface));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<SessionTokenHandler>();

        var isDevelopment = builder.Environment.IsDevelopment();
        return builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddConfigFilter<RouteGuardFilter>()
            .AddTransforms(context => UpstreamCredentialTransforms.Apply(
                context,
                routes.GetValueOrDefault(context.Route.RouteId),
                isDevelopment,
                DevUser.From(context.Services.GetRequiredService<IConfiguration>())));
    }

    /// <summary><c>--routes</c> prints what the proxy will serve.</summary>
    public static bool TryPrintLupiraBffRoutes(this WebApplicationBuilder builder, string[] args, TextWriter? output = null)
    {
        if (args is not ["--routes", ..]) return false;

        output ??= Console.Out;
        foreach (var (key, value) in ProxyRoutes.Build(ExposedSurface.Load(builder.Environment)).OrderBy(r => r.Key, StringComparer.Ordinal))
            output.WriteLine($"{key} = {value}");
        return true;
    }

    private static RouteGuardPlan GuardPlan(IServiceProvider services, ExposedSurface surface)
    {
        var plan = RouteGuards.Plan(surface, services.GetRequiredService<IOptions<RouteGuardOptions>>().Value.Specs.AsReadOnly());
        if (plan.UnguardedClusters.Count > 0)
        {
            services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RouteGuards)).LogWarning(
                "Proxy: no upstream spec for {Clusters}; their templated routes match any segment and unlisted sibling paths are not fenced",
                string.Join(", ", plan.UnguardedClusters));
        }

        return plan;
    }
}
