using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lupira.Hosting.OpenApi;

internal sealed class LupiraOperationTransformer(LupiraOpenApiOptions options) : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (options.Security is { } security && RequiresAuth(metadata))
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(security.SchemeId, document)] = new List<string>(),
            });
            if (options.UnauthorizedResponse)
                ProblemResponses.Add(operation, document, StatusCodes.Status401Unauthorized, "Unauthorized");
        }

        if (options.Idempotency is { } header && metadata.Any(header.MarkerType.IsInstanceOfType))
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = header.Name,
                In = ParameterLocation.Header,
                Required = false,
                Description = header.Description,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
            });
        }

        // The cross-cutting code no endpoint declares — ProblemExceptionHandler produces it.
        ProblemResponses.Add(operation, document, StatusCodes.Status500InternalServerError, "Internal server error");

        // Bodyless 4xx/5xx come from the non-generic arms of the typed-result unions (NotFound,
        // UnauthorizedHttpResult). UseStatusCodePages fills them at runtime, so declare the shape.
        foreach (var code in operation.Responses?.Keys.ToList() ?? [])
        {
            if (code.Length != 3 || code[0] is not ('4' or '5')) continue;
            var existing = operation.Responses![code];
            if (existing.Content is { Count: > 0 }) continue;
            operation.Responses[code] = new OpenApiResponse
            { Description = existing.Description, Content = ProblemResponses.Content(document) };
        }

        return Task.CompletedTask;
    }

    private bool RequiresAuth(IList<object> metadata) =>
        !metadata.OfType<IAllowAnonymous>().Any()
        && (options.AuthDetection == AuthDetection.NotAllowAnonymous || metadata.OfType<IAuthorizeData>().Any());
}
