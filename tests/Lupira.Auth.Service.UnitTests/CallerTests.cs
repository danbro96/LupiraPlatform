using Xunit;

namespace Lupira.Auth.Service.UnitTests;

public sealed class CallerTests
{
    private static readonly string[] Admins = AdminGroups.For("assistant");

    [Fact]
    public void Member_in_admin_group_is_admin()
    {
        var caller = Caller.Member("p@lupira.com", ["assistant-admins"], Admins);
        Assert.True(caller.IsAdmin);
        Assert.False(caller.IsService);
        Assert.Equal("p@lupira.com", caller.Actor);
    }

    [Fact]
    public void Member_without_admin_group_is_not_admin()
    {
        var caller = Caller.Member("p@lupira.com", ["users"], Admins);
        Assert.False(caller.IsAdmin);
    }

    [Fact]
    public void Another_apps_admin_group_does_not_grant_admin()
    {
        Assert.False(Caller.Member("p@lupira.com", ["comms-admins"], Admins).IsAdmin);
    }

    [Fact]
    public void Platform_admins_are_admin_in_every_app_case_insensitively()
    {
        Assert.True(Caller.Member("p@lupira.com", ["Platform-Admins"], AdminGroups.For("comms")).IsAdmin);
    }

    [Fact]
    public void Service_caller_is_never_admin_and_stamps_service_actor()
    {
        var caller = Caller.Service("cal-worker");
        Assert.True(caller.IsService);
        Assert.False(caller.IsAdmin);
        Assert.Null(caller.Email);
        Assert.Equal("service:cal-worker", caller.Actor);
    }

    [Fact]
    public void Admin_groups_for_an_app_are_its_own_plus_platform()
    {
        Assert.Equal(["tasks-admins", "platform-admins"], AdminGroups.For("tasks"));
    }
}
