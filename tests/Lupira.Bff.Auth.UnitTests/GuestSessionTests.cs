using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

public sealed class GuestSessionTests
{
    private const string GuestCookie = "__Host-lupira-tasks-guest";

    [Fact]
    public async Task Guest_cookie_reaches_the_share_surface_without_a_member_credential()
    {
        await using var host = await Start();
        var client = host.Client();
        client.DefaultRequestHeaders.Add("Cookie", await SignIn(client));

        var echo = await client.GetFromJsonAsync<UpstreamEcho>("/api/share");

        Assert.Equal("/shared/some-token", echo!.Path);
        Assert.Equal(string.Empty, echo.Authorization);
    }

    [Fact]
    public async Task Guest_cookie_does_not_reach_a_member_route()
    {
        await using var host = await Start();
        var client = host.Client();
        client.DefaultRequestHeaders.Add("Cookie", await SignIn(client));

        var res = await client.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Share_surface_is_401_without_the_guest_cookie()
    {
        await using var host = await Start();

        var res = await host.Client(AuthHost.MintToken("lupira-tasks")).GetAsync("/api/share");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Null(res.Headers.Location);
        Assert.Equal(0, host.Upstream.Hits);
    }

    private static Task<AuthHost> Start() => AuthHost.StartAsync(
        "tasks",
        "Production",
        o =>
        {
            o.EnableOidc = true;
            o.EnableBearer = true;
            o.Authority = AuthHost.Issuer;
            o.ClientId = "lupira-tasks";
            o.Audience = "lupira-tasks";
            o.CookieName = "__Host-lupira-tasks";
            o.Guest = new LupiraGuestSessionOptions { CookieName = GuestCookie, RequiredClaim = "share-token" };
        },
        app => app.MapPost("/test/guest", async (HttpContext ctx) =>
        {
            var identity = new ClaimsIdentity([new Claim("share-token", "some-token")], "Guest");
            await ctx.SignInAsync("Guest", new ClaimsPrincipal(identity));
            return TypedResults.NoContent();
        }).AllowAnonymous());

    private static async Task<string> SignIn(HttpClient client)
    {
        var res = await client.PostAsync("/test/guest", null);
        var cookie = Assert.Single(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith(GuestCookie + "=", StringComparison.Ordinal));
        return cookie.Split(';')[0];
    }
}
