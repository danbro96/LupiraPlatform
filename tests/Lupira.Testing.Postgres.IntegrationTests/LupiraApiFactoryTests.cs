using Xunit;

namespace Lupira.Testing.Postgres.IntegrationTests;

[Collection("integration")]
public sealed class LupiraApiFactoryTests(ProbeFactory factory)
{
    [Fact]
    public async Task Settings_reach_eager_and_built_configuration()
    {
        var probe = await factory.AnonymousClient().GetFromJsonAsync<SettingsProbe>("/settings");

        Assert.Equal("Development", probe!.Environment);
        Assert.Equal("https://auth.test/application/o/lupira-probe/", probe.EagerAuthority);
        Assert.Equal(factory.Authority, probe.Authority);
        Assert.Equal("extra", probe.Extra);
    }

    [Fact]
    public async Task Api_client_sends_the_dev_user()
    {
        Assert.Equal("alice@x.test", await factory.ApiClient("alice@x.test").GetStringAsync("/whoami"));
    }

    [Fact]
    public async Task Reset_applies_the_schema_once_and_wipes_data()
    {
        var api = factory.AnonymousClient();
        await factory.ResetAsync();
        await factory.ExecuteAsync("insert into notes default values");
        Assert.Equal(1, await api.GetFromJsonAsync<long>("/notes/count"));

        await factory.ResetAsync();

        Assert.Equal(0, await api.GetFromJsonAsync<long>("/notes/count"));
        Assert.Equal(1, factory.SchemaApplications);
    }
}
