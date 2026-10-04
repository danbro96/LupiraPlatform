using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Scalar.AspNetCore;

namespace Lupira.Hosting.OpenApi;

public static class OpenApiEndpoints
{
    public const string DocumentPattern = "/openapi/{documentName}.json";
    public const string ScalarPath = "/scalar";

    /// <summary>Maps <c>/openapi/{documentName}.json</c>, the Scalar UI at <c>/scalar</c> and the <c>/</c> redirect.</summary>
    public static IEndpointRouteBuilder MapLupiraOpenApi(
        this IEndpointRouteBuilder app, Action<LupiraOpenApiEndpointOptions>? configure = null)
    {
        var options = new LupiraOpenApiEndpointOptions();
        configure?.Invoke(options);

        var document = app.MapOpenApi(DocumentPattern);
        var scalar = app.MapScalarApiReference(ScalarPath, scalarOptions =>
        {
            if (options.Title is { } title) scalarOptions.WithTitle(title);
            if (options.Theme is { } theme) scalarOptions.WithTheme(theme);
        });

        if (options.AllowAnonymous)
        {
            document.AllowAnonymous();
            scalar.AllowAnonymous();
        }

        if (options.RootRedirect)
        {
            var redirect = app.MapGet("/", () => TypedResults.Redirect(ScalarPath)).ExcludeFromDescription();
            if (options.AllowAnonymous) redirect.AllowAnonymous();
        }

        return app;
    }
}
