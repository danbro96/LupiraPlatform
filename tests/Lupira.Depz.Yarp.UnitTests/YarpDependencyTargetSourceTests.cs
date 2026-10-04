using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lupira.Depz.Yarp.UnitTests;

/// <summary>The /depz roster is read from the proxy's cluster config: registry names, anonymous
/// <c>readyz</c> probes, one edge per cluster.</summary>
public class YarpDependencyTargetSourceTests
{
    private static readonly YarpDependencyTargetOptions CalBff = new()
    {
        ServiceNames =
        {
            ["cal-api"] = "lupira-cal-api",
            ["geo-api"] = "lupira-geo-api",
            ["contact-api"] = "lupira-contact-api",
            ["tasks-api"] = "lupira-tasks-api",
            ["location-api"] = "lupira-location-api",
            ["photo-api"] = "lupira-photo-api",
            ["comms-api"] = "lupira-comms-api",
        },
    };

    private static IConfiguration Clusters(params (string Cluster, string? Address)[] clusters) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(clusters.Select(c => new KeyValuePair<string, string?>(
                $"ReverseProxy:Clusters:{c.Cluster}:Destinations:primary:Address", c.Address)))
            .Build();

    [Fact]
    public void Every_cluster_maps_to_its_registry_service_name_and_address()
    {
        var targets = YarpDependencyTargetSource.From(
            Clusters(
                ("cal-api", "https://cal-api.lupira.com"),
                ("geo-api", "https://geo-api.lupira.com"),
                ("contact-api", "https://contact-api.lupira.com"),
                ("tasks-api", "https://tasks-api.lupira.com"),
                ("location-api", "https://location-api.lupira.com"),
                ("photo-api", "https://photo-api.lupira.com"),
                ("comms-api", "https://comms-api.lupira.com")),
            CalBff);

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["lupira-cal-api"] = "https://cal-api.lupira.com",
                ["lupira-geo-api"] = "https://geo-api.lupira.com",
                ["lupira-contact-api"] = "https://contact-api.lupira.com",
                ["lupira-tasks-api"] = "https://tasks-api.lupira.com",
                ["lupira-location-api"] = "https://location-api.lupira.com",
                ["lupira-photo-api"] = "https://photo-api.lupira.com",
                ["lupira-comms-api"] = "https://comms-api.lupira.com",
            },
            targets.ToDictionary(t => t.Name, t => t.BaseUrl));
    }

    [Fact]
    public void Probes_are_anonymous_readyz()
    {
        var targets = YarpDependencyTargetSource.From(
            Clusters(("cal-api", "https://cal-api.lupira.com"), ("geo-api", "http://localhost:5260")), CalBff);

        Assert.All(targets, t =>
        {
            Assert.Equal("readyz", t.ProbePath);
            Assert.Null(t.Credential);
        });
    }

    [Fact]
    public void A_cluster_without_an_address_is_kept_with_a_blank_base_url()
    {
        var target = Assert.Single(YarpDependencyTargetSource.From(Clusters(("tasks-api", "")), CalBff));

        Assert.Equal("lupira-tasks-api", target.Name);
        Assert.Equal(string.Empty, target.BaseUrl);
    }

    [Fact]
    public void An_unmapped_cluster_fails_fast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            YarpDependencyTargetSource.From(Clusters(("mystery-api", "https://x")), CalBff));

        Assert.Contains("mystery-api", ex.Message);
    }

    [Fact]
    public void Registration_binds_the_map_and_resolves_the_roster()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Clusters(("tasks-api", "http://lupira-tasks-api:8080")));
        services.AddLupiraDepzYarpTargets(o => o.ServiceNames["tasks-api"] = "lupira-tasks-api");
        using var provider = services.BuildServiceProvider();

        var target = Assert.Single(provider.GetRequiredService<IDependencyTargetSource>().GetTargets());

        Assert.Equal(("lupira-tasks-api", "http://lupira-tasks-api:8080", "readyz"), (target.Name, target.BaseUrl, target.ProbePath));
    }
}
