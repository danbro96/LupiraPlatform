using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lupira.Depz.UnitTests;

public sealed class ConfigurationDependencyTargetSourceTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => KeyValuePair.Create(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Reads_the_edge_from_the_clients_own_section()
    {
        var config = Config(
            ("Geo:BaseUrl", "https://geo-api.lupira.com"),
            ("Geo:TokenUrl", "https://auth/token"),
            ("Geo:ClientId", "cal"),
            ("Geo:ClientSecret", "s"),
            ("Geo:Scope", "geo"));

        var target = Assert.Single(ConfigurationDependencyTargetSource.From(config,
            [new ConfiguredTarget { Name = "lupira-geo-api", Section = "Geo", ProbePath = "pingz" }]));

        Assert.Equal("lupira-geo-api", target.Name);
        Assert.Equal("https://geo-api.lupira.com", target.BaseUrl);
        Assert.Equal("pingz", target.ProbePath);
        var credential = Assert.IsType<ClientCredentialsProbeCredential>(target.Credential);
        Assert.Equal(("https://auth/token", "cal", "s", "geo"), (credential.TokenUrl, credential.ClientId, credential.ClientSecret, credential.Scope));
    }

    [Fact]
    public void A_missing_section_is_kept_unconfigured_and_anonymous()
    {
        var target = Assert.Single(ConfigurationDependencyTargetSource.From(Config(),
            [new ConfiguredTarget { Name = "lupira-contact-api", Section = "Contact", ProbePath = "pingz" }]));

        Assert.Equal(string.Empty, target.BaseUrl);
        Assert.Null(target.Credential);
    }

    [Fact]
    public void A_custom_credential_and_base_url_key_override_the_defaults()
    {
        var config = Config(("Gpt:Url", "https://gpt-api.lupira.com"), ("Gpt:ApiKey", "k"));

        var target = Assert.Single(ConfigurationDependencyTargetSource.From(config,
        [
            new ConfiguredTarget
            {
                Name = "gpt-api",
                Section = "Gpt",
                ProbePath = "v1/models",
                BaseUrlKey = "Url",
                Credential = s => StaticHeaderProbeCredential.Bearer(s["ApiKey"]!),
            },
        ]));

        Assert.Equal("https://gpt-api.lupira.com", target.BaseUrl);
        Assert.Equal("Bearer k", Assert.IsType<StaticHeaderProbeCredential>(target.Credential).Value);
    }
}
