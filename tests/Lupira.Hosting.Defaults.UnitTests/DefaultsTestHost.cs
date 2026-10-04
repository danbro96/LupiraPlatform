using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Lupira.Hosting.Defaults.UnitTests;

internal static class DefaultsTestHost
{
    public static async Task<WebApplication> StartAsync(Action<LupiraDefaultsOptions>? configure = null, Dictionary<string, string?>? settings = null)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { ApplicationName = "SampleApi", EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        if (settings is not null)
            builder.Configuration.AddInMemoryCollection(settings);
        builder.AddLupiraDefaults(configure);
        var app = builder.Build();
        app.UseLupiraDefaults();
        app.MapPost("/samples", (Sample sample) => TypedResults.Ok(sample));
        app.MapGet("/sample", () => TypedResults.Ok(new Sample
        {
            Name = "x",
            Day = DayOfWeek.Friday,
            At = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.FromHours(2)),
        }));
        app.MapGet("/request", (HttpContext ctx) => TypedResults.Ok(new[]
        {
            ctx.Request.Scheme,
            ctx.Request.Host.Value,
            ctx.Connection.RemoteIpAddress?.ToString(),
        }));
        app.MapGet("/mcp/missing", () => TypedResults.NotFound());
        app.MapGet("/internal/missing", () => TypedResults.NotFound());
        await app.StartAsync();
        return app;
    }
}
