using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task User_is_401_when_anonymous()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client().GetAsync("/auth/user")).StatusCode);
    }

    [Fact]
    public async Task User_reads_the_session_not_a_bearer()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client(AuthHost.MintToken()).GetAsync("/auth/user");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task User_matches_admin_groups_case_insensitively()
    {
        await using var host = await AuthHost.StartAsync("cal", "Development", AuthHost.Cal, settings: new Dictionary<string, string>
        {
            ["Dev:Groups:0"] = "family",
            ["Dev:Groups:1"] = "Platform-Admins",
        });

        var user = await host.Client().GetFromJsonAsync<UserInfo>("/auth/user");

        Assert.Equal(["family", "Platform-Admins"], user!.Groups);
        Assert.True(user.IsAdmin);
    }

    [Fact]
    public async Task Development_runs_as_the_configured_user_with_the_default_dev_groups()
    {
        await using var host = await AuthHost.StartAsync("cal", "Development", AuthHost.Cal, settings: new Dictionary<string, string>
        {
            ["Dev:User"] = "dev@test",
        });

        var user = await host.Client().GetFromJsonAsync<UserInfo>("/auth/user");

        Assert.Equal("dev@test", user!.Email);
        Assert.Equal(["cal-admins"], user.Groups);
        Assert.True(user.IsAdmin);
    }

    [Fact]
    public async Task Development_groups_come_from_config_and_satisfy_a_required_group()
    {
        await using var host = await AuthHost.StartAsync("cal", "Development", AuthHost.Cal, settings: new Dictionary<string, string>
        {
            ["Dev:Groups:0"] = "family",
            ["Auth:RequiredGroup"] = "members",
        });

        var user = await host.Client().GetFromJsonAsync<UserInfo>("/auth/user");

        Assert.Equal(["family", "members"], user!.Groups);
        Assert.False(user.IsAdmin);
    }

    [Theory]
    [InlineData("/calendar?day=1", "/calendar?day=1")]
    [InlineData("https://evil.example/x", "/")]
    [InlineData("//evil.example/x", "/")]
    [InlineData("/\\evil.example", "/")]
    [InlineData("calendar", "/")]
    [InlineData(null, "/")]
    public async Task Login_only_returns_to_a_same_site_path(string? returnUrl, string expected)
    {
        await using var host = await AuthHost.StartAsync("cal", "Development", AuthHost.Cal);

        var res = await host.Client().GetAsync(returnUrl is null ? "/auth/login" : $"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal(expected, res.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Logout_without_oidc_returns_to_the_root()
    {
        await using var host = await AuthHost.StartAsync("cal", "Development", AuthHost.Cal);

        var res = await host.Client().PostAsync("/auth/logout", null);

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/", res.Headers.Location!.OriginalString);
    }
}
