using Scalar.AspNetCore;

namespace Lupira.Hosting.OpenApi;

/// <summary>Endpoints mapped by <see cref="OpenApiEndpoints.MapLupiraOpenApi"/>.</summary>
public sealed class LupiraOpenApiEndpointOptions
{
    /// <summary>Scalar page title; null keeps Scalar's.</summary>
    public string? Title { get; set; }

    /// <summary>Null keeps Scalar's default theme.</summary>
    public ScalarTheme? Theme { get; set; } = ScalarTheme.BluePlanet;

    /// <summary>Redirects <c>/</c> to <c>/scalar</c>.</summary>
    public bool RootRedirect { get; set; } = true;

    public bool AllowAnonymous { get; set; } = true;
}
