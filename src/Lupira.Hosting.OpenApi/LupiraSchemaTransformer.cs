using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lupira.Hosting.OpenApi;

internal sealed class LupiraSchemaTransformer(LupiraOpenApiOptions options) : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (options.DateTimeOffsetAsString)
        {
            var type = context.JsonTypeInfo.Type;
            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                schema.Type = type == typeof(DateTimeOffset?)
                    ? JsonSchemaType.String | JsonSchemaType.Null
                    : JsonSchemaType.String;
                schema.Format = "date-time";
            }
        }

        if (options.DropNullEnumMembers && schema.Enum is { Count: > 0 } members)
        {
            for (var i = members.Count - 1; i >= 0; i--)
            {
                if (members[i] is null || members[i]!.GetValueKind() == JsonValueKind.Null)
                    members.RemoveAt(i);
            }
        }

        return Task.CompletedTask;
    }
}
