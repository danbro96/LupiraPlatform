using System.Globalization;
using Microsoft.OpenApi;

namespace Lupira.Hosting.OpenApi;

internal static class ProblemResponses
{
    public const string SchemaId = "ProblemDetails";

    // Every error response carries the same shape, so a generated client types its error once instead of
    // falling back to `void`.
    public static Dictionary<string, OpenApiMediaType> Content(OpenApiDocument? document) =>
        new() { ["application/problem+json"] = new() { Schema = new OpenApiSchemaReference(SchemaId, document) } };

    public static void Add(OpenApiOperation operation, OpenApiDocument? document, int status, string description)
    {
        var code = status.ToString(CultureInfo.InvariantCulture);
        operation.Responses ??= [];
        if (operation.Responses.ContainsKey(code)) return;
        operation.Responses[code] = new OpenApiResponse { Description = description, Content = Content(document) };
    }

    // RFC 9457. Declared here because nothing returns the CLR type directly, so the generator never emits it.
    public static OpenApiSchema Schema() => new()
    {
        Type = JsonSchemaType.Object,
        Description = "RFC 9457 problem details.",
        Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["type"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer | JsonSchemaType.Null, Format = "int32" },
            ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
        },
    };
}
