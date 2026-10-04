using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Lupira.Hosting.Observability.UnitTests;

public sealed class TelemetryRegistrationTests
{
    private static HostApplicationBuilder Builder(string environment, string? otlpEndpoint)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "SampleBff",
            EnvironmentName = environment,
        });
        if (otlpEndpoint is not null)
            builder.Configuration.AddInMemoryCollection([new(TelemetryHostApplicationBuilderExtensions.OtlpEndpointKey, otlpEndpoint)]);
        return builder;
    }

    [Fact]
    public void Without_an_endpoint_a_deployed_service_fails_startup()
    {
        var builder = Builder(Environments.Production, null);

        Assert.Throws<InvalidOperationException>(() => builder.AddLupiraTelemetry("sample-web"));
    }

    [Fact]
    public void Without_an_endpoint_development_registers_nothing()
    {
        var builder = Builder(Environments.Development, null);
        builder.AddLupiraTelemetry("sample-web");
        using var host = builder.Build();

        Assert.Null(host.Services.GetService<TracerProvider>());
        Assert.Null(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void Development_console_export_is_opt_in()
    {
        var plain = Builder(Environments.Development, null);
        plain.AddLupiraTelemetry("sample-web");
        using var plainHost = plain.Build();

        var console = Builder(Environments.Development, null);
        console.AddLupiraTelemetry("sample-web", o => o.ConsoleInDevelopment = true);
        using var consoleHost = console.Build();

        Assert.Null(plainHost.Services.GetService<TracerProvider>());
        Assert.NotNull(consoleHost.Services.GetService<TracerProvider>());
    }

    [Fact]
    public void An_endpoint_registers_traces_and_metrics()
    {
        var builder = Builder(Environments.Production, "http://127.0.0.1:9");
        builder.AddLupiraTelemetry("sample-web");
        using var host = builder.Build();

        Assert.NotNull(host.Services.GetService<TracerProvider>());
        Assert.NotNull(host.Services.GetService<MeterProvider>());
    }

    [Fact]
    public void App_and_platform_meters_export_without_registration()
    {
        var exporter = new MeterNameExporter();
        var builder = Builder(Environments.Production, "http://127.0.0.1:9");
        builder.AddLupiraTelemetry("sample-web", o => o.Meters.Add("Extra.Meter"));
        builder.Services.ConfigureOpenTelemetryMeterProvider(m => m.AddReader(new BaseExportingMetricReader(exporter)));
        using var host = builder.Build();
        var provider = host.Services.GetRequiredService<MeterProvider>();

        using var app = new Meter("SampleBff.Depz");
        using var platform = new Meter("Lupira.Depz");
        using var extra = new Meter("Extra.Meter");
        using var unrelated = new Meter("Unrelated.Meter");
        foreach (var meter in new[] { app, platform, extra, unrelated })
            meter.CreateCounter<int>("probes").Add(1);
        provider.ForceFlush();

        Assert.Contains("SampleBff.Depz", exporter.MeterNames);
        Assert.Contains("Lupira.Depz", exporter.MeterNames);
        Assert.Contains("Extra.Meter", exporter.MeterNames);
        Assert.DoesNotContain("Unrelated.Meter", exporter.MeterNames);
    }
}
