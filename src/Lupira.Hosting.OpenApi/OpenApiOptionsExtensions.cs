using Microsoft.AspNetCore.OpenApi;

namespace Lupira.Hosting.OpenApi;

public static class OpenApiOptionsExtensions
{
    /// <summary>Adds the Lupira security scheme, ProblemDetails error responses and the opted-in schema fixes.
    /// Call inside the app's own <c>AddOpenApi</c> so the XML-comment generator still intercepts it.</summary>
    public static OpenApiOptions AddLupiraConventions(
        this OpenApiOptions options, Action<LupiraOpenApiOptions>? configure = null)
    {
        var lupira = new LupiraOpenApiOptions();
        configure?.Invoke(lupira);

        options.AddDocumentTransformer(new LupiraDocumentTransformer(lupira));
        if (lupira.DateTimeOffsetAsString || lupira.DropNullEnumMembers)
            options.AddSchemaTransformer(new LupiraSchemaTransformer(lupira));
        foreach (var transformer in lupira.OperationTransformers)
            options.AddOperationTransformer(transformer);
        options.AddOperationTransformer(new LupiraOperationTransformer(lupira));
        return options;
    }
}
