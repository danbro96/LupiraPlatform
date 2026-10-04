using System.Net;
using Lupira.Contracts.Depz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Lupira.Depz.UnitTests;

public sealed class DependencyPollWorkerTests
{
    [Fact]
    public async Task A_sweep_publishes_every_target_under_the_service_name()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLupiraDepz(o =>
        {
            o.ProbeKey = "k";
            o.ServiceName = "lupira-test-api";
            o.MetricPrefix = "test";
        });
        services.AddLupiraDepzTargets(
        [
            new DependencyTarget { Name = "a", BaseUrl = "http://a", ProbePath = "readyz" },
            new DependencyTarget { Name = "b", BaseUrl = string.Empty, ProbePath = "readyz" },
        ]);
        services.AddHttpClient(DependencyProbe.ProbeClientName)
            .ConfigurePrimaryHttpMessageHandler(() => StubHandler.Status(HttpStatusCode.OK));
        using var provider = services.BuildServiceProvider();
        var worker = Assert.IsType<DependencyPollWorker>(Assert.Single(provider.GetServices<IHostedService>()));

        await worker.SweepSafelyAsync(CancellationToken.None);

        var report = provider.GetRequiredService<DependencyReportCache>().Current();
        Assert.Equal("lupira-test-api", report.Service);
        Assert.NotNull(report.LastPolledUtc);
        Assert.Equal(
            [("a", DependencyStatus.Healthy), ("b", DependencyStatus.Unconfigured)],
            report.Dependencies.Select(d => (d.Name, d.Status)));
    }

    [Fact]
    public void Options_without_a_service_name_fail_validation()
    {
        var services = new ServiceCollection();
        services.AddLupiraDepz(o => o.MetricPrefix = "test");
        using var provider = services.BuildServiceProvider();

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<DependencyReportCache>());
    }
}
