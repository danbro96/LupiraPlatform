using Xunit;

namespace Lupira.Testing.Postgres.UnitTests;

public sealed class HostSettingsTests
{
    [Fact]
    public void Connection_string_goes_under_the_named_key()
    {
        var settings = HostSettings.Compose("tasks", "Host=db", authentikSlug: null);

        Assert.Equal("Host=db", settings["ConnectionStrings:tasks"]);
        Assert.False(settings.ContainsKey("Auth:Oidc:Authority"));
    }

    [Fact]
    public void Slug_names_a_test_authority()
    {
        var settings = HostSettings.Compose("Postgres", "Host=db", "lupira-cal");

        Assert.Equal("https://auth.test/application/o/lupira-cal/", settings["Auth:Oidc:Authority"]);
    }
}
