using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Bff.OpenApi;

public static class LupiraBffOpenApiExtensions
{
    public static IServiceCollection AddLupiraBffOpenApi(
        this IServiceCollection services, Action<UpstreamSpecMergerOptions> configure, string documentName = "v1")
    {
        services.Configure(configure);
        return services.AddOpenApi(documentName, options => options.AddDocumentTransformer<BffDocumentTransformer>());
    }
}
