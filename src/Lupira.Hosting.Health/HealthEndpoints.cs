using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lupira.Hosting.Health;

public static class HealthEndpoints
{
    /// <summary>Anonymous <c>/livez</c> (live-tagged) and <c>/readyz</c> (ready-tagged; healthy when none are
    /// registered), kept out of HTTP metrics.</summary>
    public static IEndpointRouteBuilder MapLupiraHealth(this IEndpointRouteBuilder app)
    {
        // Detailed per-dependency JSON only outside Production — the body reveals dependency
        // topology and these probes are anonymous.
        var detailed = !app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsProduction();

        app.MapHealthChecks("/livez", Options(HealthTags.Live, detailed))
            .AllowAnonymous()
            .DisableHttpMetrics();

        app.MapHealthChecks("/readyz", Options(HealthTags.Ready, detailed))
            .AllowAnonymous()
            .DisableHttpMetrics();

        return app;
    }

    private static HealthCheckOptions Options(string tag, bool detailed)
    {
        var options = new HealthCheckOptions { Predicate = check => check.Tags.Contains(tag) };
        if (detailed)
            options.ResponseWriter = HealthReportWriter.WriteJsonAsync;
        return options;
    }
}
