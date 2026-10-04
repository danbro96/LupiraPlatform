using System.Reflection;
using Lupira.Bff.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lupira.Bff.OpenApi;

public static class LupiraBffOpenApiExtensions
{
    public static IServiceCollection AddLupiraBffOpenApi(
        this IServiceCollection services, Action<UpstreamSpecMergerOptions> configure, string documentName = "v1")
    {
        services.Configure(configure);
        services.AddOptions<RouteGuardOptions>()
            .Configure<IOptions<UpstreamSpecMergerOptions>, IHostEnvironment>((guards, merger, environment) =>
            {
                var assembly = Assembly.Load(new AssemblyName(environment.ApplicationName));
                foreach (var upstream in merger.Value.Upstreams)
                    guards.Specs.TryAdd(upstream.Cluster, upstream.Load(assembly).Document);
            });
        return services.AddOpenApi(documentName, options => options.AddDocumentTransformer<BffDocumentTransformer>());
    }
}
