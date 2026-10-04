using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lupira.Hosting.OpenApi;

/// <summary>Document conventions applied by <see cref="OpenApiOptionsExtensions.AddLupiraConventions"/>.</summary>
public sealed class LupiraOpenApiOptions
{
    public const string DefaultIdempotencyHeaderName = "Idempotency-Key";

    /// <summary>Replaces the generated info block when set; null keeps the generator's.</summary>
    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>Null declares no scheme and marks no operation as secured.</summary>
    public ApiSecurity? Security { get; set; } = ApiSecurity.Bearer();

    public AuthDetection AuthDetection { get; set; } = AuthDetection.AuthorizeData;

    /// <summary>Adds a problem+json 401 to secured operations that don't declare one.</summary>
    public bool UnauthorizedResponse { get; set; } = true;

    /// <summary>A nullable use of an enum makes the framework append null to the shared component schema,
    /// although the property's own oneOf already carries the nullability. Generators read that null onto the
    /// enum type itself, so non-nullable uses inherit it too.</summary>
    public bool DropNullEnumMembers { get; set; }

    /// <summary>A custom DateTimeOffset converter hides the CLR type from schema inference, which would
    /// otherwise emit <c>format: date-time</c> with no <c>type</c> at all.</summary>
    public bool DateTimeOffsetAsString { get; set; }

    /// <summary>Run before the built-in operation transformer, in order.</summary>
    public IList<Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task>> OperationTransformers { get; } = [];

    internal MarkerHeader? Idempotency { get; private set; }

    /// <summary>Documents an optional uuid header on operations whose endpoint metadata carries a <typeparamref name="TMarker"/>.</summary>
    public LupiraOpenApiOptions IdempotencyHeader<TMarker>(string description, string name = DefaultIdempotencyHeaderName)
    {
        Idempotency = new MarkerHeader(typeof(TMarker), name, description);
        return this;
    }
}
