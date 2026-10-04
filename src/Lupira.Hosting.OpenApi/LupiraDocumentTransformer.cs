using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lupira.Hosting.OpenApi;

internal sealed class LupiraDocumentTransformer(LupiraOpenApiOptions options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (options.Title is { } title)
            document.Info = new() { Title = title, Version = context.DocumentName, Description = options.Description };

        document.Components ??= new();
        if (options.Security is { } security)
        {
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[security.SchemeId] = security.CreateScheme();
        }

        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        document.Components.Schemas[ProblemResponses.SchemaId] = ProblemResponses.Schema();
        return Task.CompletedTask;
    }
}
