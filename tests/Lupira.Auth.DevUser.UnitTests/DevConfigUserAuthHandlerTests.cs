using Xunit;

namespace Lupira.Auth.DevUser.UnitTests;

public sealed class DevConfigUserAuthHandlerTests
{
    [Fact]
    public async Task Without_config_the_defaults_apply()
    {
        await using var app = await DevAuthTestHost.StartAsync(a => a.AddLupiraDevConfigUserAuth(configure: o => o.DefaultGroups = ["cal-admins"]));

        var who = await app.WhoAsync();

        Assert.Equal("success", who.Outcome);
        Assert.Equal("dev@localhost", who.Name);
        Assert.Equal(["sub=dev|dev@localhost", "email=dev@localhost", "groups=cal-admins", "name=dev@localhost"], who.Claims);
    }

    [Fact]
    public async Task Configured_user_and_groups_win()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Dev:User"] = "owner@lupira.se",
            ["Dev:Groups:0"] = "family",
            ["Dev:Groups:1"] = "tasks-admins",
        };
        await using var app = await DevAuthTestHost.StartAsync(a => a.AddLupiraDevConfigUserAuth(configure: o => o.DefaultGroups = ["cal-admins"]), settings);

        var who = await app.WhoAsync();

        Assert.Equal("owner@lupira.se", who.Name);
        Assert.Equal(["family", "tasks-admins"], who.Groups);
    }

    [Fact]
    public async Task No_default_groups_means_none()
    {
        await using var app = await DevAuthTestHost.StartAsync(a => a.AddLupiraDevConfigUserAuth());

        var who = await app.WhoAsync();

        Assert.Empty(who.Groups);
    }
}
