using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Lupira.Hosting.Observability;

public static class TelemetryHostApplicationBuilderExtensions
{
    public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>Traces, metrics and logs over OTLP; outside Development a missing <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>
    /// fails startup. <c>Lupira.*</c> and <c>&lt;ApplicationName&gt;.*</c> meters/sources are always collected.</summary>
    public static IHostApplicationBuilder AddLupiraTelemetry(
        this IHostApplicationBuilder builder, string serviceName, Action<LupiraTelemetryOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        var options = new LupiraTelemetryOptions();
        configure?.Invoke(options);

        if (IsDocumentGeneration())
            return builder;

        var otlp = !string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]);
        if (!otlp && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException($"{OtlpEndpointKey} is not set; every deployed service exports telemetry.");
        var console = !otlp && options.ConsoleInDevelopment && builder.Environment.IsDevelopment();
        if (!otlp && !console)
            return builder;

        var appPrefix = builder.Environment.ApplicationName + ".*";
        string[] meters = ["Lupira.*", appPrefix, .. options.Meters];
        string[] sources = ["Lupira.*", appPrefix, .. options.Sources];
        var version = options.ServiceVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "0.0.0";

        // Endpoint stays out of code: setting it disables the /v1/{signal} path append under http/protobuf.
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceVersion: version))
            .WithTracing(t =>
            {
                t.AddSource(sources)
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = SpanFilter.For(options.FilteredPaths);
                    })
                    .AddHttpClientInstrumentation();
                if (otlp)
                    t.AddOtlpExporter();
                else
                    t.AddConsoleExporter();
            })
            .WithMetrics(m =>
            {
                m.AddMeter(meters)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (otlp)
                    m.AddOtlpExporter();
                else
                    m.AddConsoleExporter();
            })
            .WithLogging(
                l =>
                {
                    if (otlp)
                        l.AddOtlpExporter();
                    else
                        l.AddConsoleExporter();
                },
                o =>
                {
                    o.IncludeFormattedMessage = true;
                    o.IncludeScopes = true;
                });

        return builder;
    }

    // Build-time OpenAPI generation boots the app inside this tool, with no deployment config.
    private static bool IsDocumentGeneration() =>
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
}
