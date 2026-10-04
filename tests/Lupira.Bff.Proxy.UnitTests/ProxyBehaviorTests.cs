using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Lupira.Bff.Proxy.UnitTests;

/// <summary>
/// The proxy is an allowlist: only the VERB+path pairs in exposed.json reach the upstream. These prove the
/// running app behaves — a miss 404s instead of serving the SPA shell, and each group carries exactly the
/// credential it declares.
/// </summary>
public sealed class ProxyBehaviorTests
{
    private const string DeviceKey = "DeviceKey 0123456789abcdef0123456789abcdef.0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("/api/items", "/items")]
    [InlineData("/photo-api/photos", "/photos")]
    [InlineData("/comms-api/topics/1", "/topics/1")]
    [InlineData("/geo-api/basemap/fonts/Noto/0-255.pbf", "/basemap/fonts/Noto/0-255.pbf")]
    public async Task Allowlisted_path_proxies_with_its_prefix_stripped_and_the_bearer_verbatim(string path, string upstreamPath)
    {
        await using var host = await ProxyHost.StartAsync("cal");

        var echo = await host.Client("member-token").GetFromJsonAsync<UpstreamEcho>(path);

        Assert.Equal(upstreamPath, echo!.Path);
        Assert.Equal("Bearer member-token", echo.Authorization);
        Assert.Equal(string.Empty, echo.XDevUser);
    }

    [Theory]
    [InlineData("/api/mcp")]
    [InlineData("/api/internal/items")]
    [InlineData("/api/dav-backend/u/someone/collections")]
    [InlineData("/api/openapi/v1.json")]
    [InlineData("/api/scalar")]
    [InlineData("/api/pingz")]
    [InlineData("/tasks-api/shared/abc")]
    [InlineData("/tasks-api/users/directory")]
    [InlineData("/comms-api/ingest")]
    public async Task Unlisted_path_is_404_and_never_reaches_the_upstream(string path)
    {
        await using var host = await ProxyHost.StartAsync("cal");

        var res = await host.Client("member-token").GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Theory]
    [InlineData("PUT", "/api/calendars")]
    [InlineData("POST", "/geo-api/basemap/style.json")]
    public async Task Wrong_verb_on_an_allowlisted_path_is_404_not_the_spa_shell(string verb, string path)
    {
        await using var host = await ProxyHost.StartAsync("cal");

        var res = await host.Client("member-token").SendAsync(new HttpRequestMessage(new HttpMethod(verb), path) { Content = new StringContent("{}") });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.NotEqual("text/html", res.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Development_replaces_a_caller_supplied_dev_user_rather_than_appending()
    {
        await using var host = await ProxyHost.StartAsync("cal", "Development");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/items");
        request.Headers.TryAddWithoutValidation(DevUser.HeaderName, "intruder@test");
        request.Headers.TryAddWithoutValidation(TestAuthHandler.ShareHeader, "signed-in");

        var res = await host.Client().SendAsync(request);
        var echo = await res.Content.ReadFromJsonAsync<UpstreamEcho>();

        Assert.Equal(ProxyHost.DevUserName, echo!.XDevUser);
    }

    [Fact]
    public async Task Production_never_invents_a_dev_identity()
    {
        await using var host = await ProxyHost.StartAsync("cal");

        var echo = await host.Client("member-token").GetFromJsonAsync<UpstreamEcho>("/contact-api/me");

        Assert.Equal(string.Empty, echo!.XDevUser);
    }

    [Fact]
    public async Task Unauthenticated_member_call_is_rejected_before_the_upstream()
    {
        await using var host = await ProxyHost.StartAsync("cal");

        var res = await host.Client().GetAsync("/photo-api/photos");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Device_ingest_forwards_a_well_formed_key_untouched_at_the_upstream_path()
    {
        await using var host = await ProxyHost.StartAsync("cal", "Development");
        var client = host.Client();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", DeviceKey);

        var res = await client.PostAsync("/ingest/location", new StringContent("{}\n", null, "application/x-ndjson"));
        var echo = await res.Content.ReadFromJsonAsync<UpstreamEcho>();

        Assert.Equal("/ingest/location", echo!.Path);
        Assert.Equal(DeviceKey, echo.Authorization);
        Assert.Equal(string.Empty, echo.XDevUser);
    }

    [Fact]
    public async Task Unlisted_path_under_the_device_mount_is_404_even_with_a_well_formed_key()
    {
        await using var host = await ProxyHost.StartAsync("cal");
        var client = host.Client();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", DeviceKey);

        var res = await client.PostAsync("/ingest/comms", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("DeviceKey not-a-key")]
    [InlineData("Bearer member-token")]
    public async Task Device_ingest_rejects_a_missing_or_malformed_key(string? authorization)
    {
        await using var host = await ProxyHost.StartAsync("cal");
        var client = host.Client();
        if (authorization is not null) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);

        var res = await client.PostAsync("/ingest/location", new StringContent("{}\n"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Guest_route_replays_the_token_from_the_claim_without_a_member_credential()
    {
        await using var host = await ProxyHost.StartAsync("tasks");
        var client = host.Client("member-token");
        client.DefaultRequestHeaders.TryAddWithoutValidation(TestAuthHandler.ShareHeader, "some-token");

        var res = await client.PostAsync("/api/share/items/42/complete", new StringContent("{}"));
        var echo = await res.Content.ReadFromJsonAsync<UpstreamEcho>();

        // The token left the URL at the exchange and comes back from the session here.
        Assert.Equal("/shared/some-token/items/42/complete", echo!.Path);
        // Attaching a member token would widen the link to that member.
        Assert.Equal(string.Empty, echo.Authorization);
        Assert.Equal(string.Empty, echo.XDevUser);
    }

    [Fact]
    public async Task Share_surface_is_unreachable_without_the_share_claim()
    {
        await using var host = await ProxyHost.StartAsync("tasks");

        var res = await host.Client("member-token").GetAsync("/api/share");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Redeem_stays_member_authed_despite_sitting_next_to_the_share_surface()
    {
        await using var host = await ProxyHost.StartAsync("tasks");
        var client = host.Client();

        var res = await client.PostAsync("/api/shares/redeem", JsonContent.Create(new { token = "x" }));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Announcing_cluster_carries_its_prefix_and_others_do_not()
    {
        await using var host = await ProxyHost.StartAsync("assistant");
        var client = host.Client("member-token");

        var assistant = await client.GetFromJsonAsync<UpstreamEcho>("/api/me/profile");
        var comms = await client.GetFromJsonAsync<UpstreamEcho>("/comms-api/topics?status=released");

        Assert.Equal("/me/profile", assistant!.Path);
        Assert.Equal("/api", assistant.XForwardedPrefix);
        Assert.Equal("/topics", comms!.Path);
        Assert.Equal(string.Empty, comms.XForwardedPrefix);
    }

    [Fact]
    public async Task Anonymous_group_is_reachable_without_a_credential()
    {
        await using var host = await ProxyHost.StartAsync("assistant");

        var echo = await host.Client().GetFromJsonAsync<UpstreamEcho>("/api/auth/login?return_uri=x");

        Assert.Equal("/auth/login", echo!.Path);
        Assert.Equal("/api", echo.XForwardedPrefix);
        Assert.Equal(string.Empty, echo.Authorization);
    }
}
