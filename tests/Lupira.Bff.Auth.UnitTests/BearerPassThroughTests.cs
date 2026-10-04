using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Lupira.Bff.Auth.UnitTests;

/// <summary>The two front doors of the member proxy: a caller-presented bearer authenticates and is forwarded
/// verbatim to every upstream; anonymous API calls get status codes (never Authentik redirects) on every
/// proxied prefix; garbage bearers are rejected at the BFF.</summary>
public sealed partial class BearerPassThroughTests
{
    [Theory]
    [InlineData("/api/items", "/items")]
    [InlineData("/geo-api/places", "/places")]
    [InlineData("/contact-api/contacts", "/contacts")]
    [InlineData("/tasks-api/items", "/items")]
    [InlineData("/location-api/location/visits", "/location/visits")]
    [InlineData("/photo-api/photos", "/photos")]
    [InlineData("/comms-api/topics", "/topics")]
    public async Task Valid_bearer_is_accepted_and_forwarded_verbatim(string path, string upstreamPath)
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);
        var token = AuthHost.MintToken();

        var res = await host.Client(token).GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var echo = (await res.Content.ReadFromJsonAsync<UpstreamEcho>())!;
        Assert.Equal(upstreamPath, echo.Path);
        Assert.Equal($"Bearer {token}", echo.Authorization);   // the caller's token, untouched — not a cookie exchange
        Assert.Equal(string.Empty, echo.XDevUser);              // production never sends the dev header
    }

    [Fact]
    public async Task Every_member_route_answers_an_anonymous_call_with_401_not_a_redirect()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);
        var client = host.Client();
        var member = host.Surface.Operations.Where(o => o.Group.Policy == "Default").ToList();

        Assert.Contains(member, o => o.BffPath.StartsWith("/photo-api/", StringComparison.Ordinal));
        Assert.Contains(member, o => o.BffPath.StartsWith("/comms-api/", StringComparison.Ordinal));
        foreach (var operation in member)
        {
            var path = Parameter().Replace(CatchAll().Replace(operation.BffPath, "fonts/a/0-255.pbf"), "00000000-0000-0000-0000-000000000001");
            var res = await client.SendAsync(new HttpRequestMessage(new HttpMethod(operation.Verb), path));

            Assert.True(res.StatusCode == HttpStatusCode.Unauthorized, $"{operation.Verb} {path}: {(int) res.StatusCode}");
            Assert.Null(res.Headers.Location);
        }

        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Wrong_audience_bearer_is_rejected()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client(AuthHost.MintToken(audience: "someone-else")).GetAsync("/api/items");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Garbage_bearer_is_rejected()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client("not-a-jwt").GetAsync("/api/items");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Anonymous_page_navigation_still_redirects_to_sign_in()
    {
        await using var host = await AuthHost.StartAsync("cal", "Production", AuthHost.Cal);

        var res = await host.Client().GetAsync("/auth/login");

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.StartsWith($"{AuthHost.Issuer}authorize", res.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bearer_only_production_rejects_a_missing_or_foreign_token()
    {
        await using var host = await AuthHost.StartAsync("assistant", "Production", o =>
        {
            o.EnableBearer = true;
            o.BearerAuthority = AuthHost.Issuer;
            o.Audience = "lupira-assistant";
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client().GetAsync("/api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client(AuthHost.MintToken("some-other-api")).GetAsync("/api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.Client(AuthHost.MintToken("lupira-assistant")).GetAsync("/api/me/profile")).StatusCode);
    }

    [GeneratedRegex(@"\{\w+\}")]
    private static partial Regex Parameter();

    [GeneratedRegex(@"\{\*\*\w+\}")]
    private static partial Regex CatchAll();
}
