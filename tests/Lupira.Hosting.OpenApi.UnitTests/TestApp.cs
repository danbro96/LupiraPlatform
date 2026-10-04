using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lupira.Hosting.OpenApi.UnitTests;

internal static class TestApp
{
    public static async Task<WebApplication> StartAsync(
        Action<LupiraOpenApiOptions>? conventions = null,
        Action<LupiraOpenApiEndpointOptions>? endpoints = null,
        bool fallbackPolicy = false,
        bool opaqueDateTimeOffset = false)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { ApplicationName = "Widgets" });
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            if (opaqueDateTimeOffset)
                o.SerializerOptions.Converters.Add(new OpaqueDateTimeOffsetConverter());
        });
        builder.Services.AddAuthentication(AnonymousAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, AnonymousAuthHandler>(AnonymousAuthHandler.SchemeName, null);
        var authorization = builder.Services.AddAuthorizationBuilder();
        if (fallbackPolicy)
            authorization.SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddOpenApi("v1", options => options.AddLupiraConventions(conventions));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLupiraOpenApi(endpoints);
        MapWidgets(app);
        await app.StartAsync();
        return app;
    }

    public static async Task<string> DocumentAsync(this WebApplication app) =>
        await app.GetTestClient().GetStringAsync("/openapi/v1.json");

    private static void MapWidgets(WebApplication app)
    {
        app.MapGet("/widgets/{id:guid}", Results<Ok<WidgetDto>, NotFound> (Guid id) => TypedResults.NotFound())
            .RequireAuthorization();
        app.MapGet("/widgets", Ok<WidgetDto[]> (WidgetSort? sort) => TypedResults.Ok(Array.Empty<WidgetDto>()))
            .RequireAuthorization();
        app.MapPost("/widgets", Created<WidgetDto> (WidgetDto widget) => TypedResults.Created("/widgets", widget))
            .RequireAuthorization()
            .WithMetadata(new IdempotentMutation());
        app.MapGet("/shared/{token}/widgets", Ok<WidgetDto[]> () => TypedResults.Ok(Array.Empty<WidgetDto>()));
        app.MapGet("/public", Ok<string> () => TypedResults.Ok("hi")).AllowAnonymous();
    }
}
