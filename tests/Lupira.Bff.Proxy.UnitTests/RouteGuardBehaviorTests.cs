using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lupira.Bff.Proxy.UnitTests;

/// <summary>
/// A templated route forwards only what the upstream would route to that operation: an unlisted sibling
/// literal never reaches the upstream, whether the type constraint or the fence stops it.
/// </summary>
public sealed class RouteGuardBehaviorTests
{
    private const string Id = "0b6f2c1e-8a9d-4e57-9f3a-2d4c6b8e1a07";

    public static TheoryData<string, bool> UnlistedSiblings => new()
    {
        { "/api/items/thin", true },
        { "/api/items/thin", false },
        { "/photo-api/photos/density", true },
        { "/photo-api/photos/density", false },
        { "/geo-api/places/duplicates", true },
        { "/geo-api/places/duplicates", false },
        { "/contact-api/contacts/thin", false },
    };

    public static TheoryData<string, string, bool> Listed => new()
    {
        { "/photo-api/photos/map", "/photos/map", true },
        { "/photo-api/photos/albums", "/photos/albums", false },
        { "/geo-api/places/suggest", "/places/suggest", true },
        { $"/api/items/{Id}", $"/items/{Id}", true },
        { $"/api/items/{Id}", $"/items/{Id}", false },
        { $"/photo-api/photos/{Id}", $"/photos/{Id}", true },
        { $"/geo-api/places/{Id}/history", $"/places/{Id}/history", true },
        { $"/comms-api/topics/{Id}", $"/topics/{Id}", true },
    };

    [Theory]
    [MemberData(nameof(UnlistedSiblings))]
    public async Task Unlisted_sibling_literal_is_404_and_never_reaches_the_upstream(string path, bool typed)
    {
        await using var host = await ProxyHost.StartAsync("cal", specs: Specs(typed));

        var res = await host.Client("member-token").GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Theory]
    [MemberData(nameof(Listed))]
    public async Task Listed_literals_and_valid_ids_still_proxy(string path, string upstreamPath, bool typed)
    {
        await using var host = await ProxyHost.StartAsync("cal", specs: Specs(typed));

        var echo = await host.Client("member-token").GetFromJsonAsync<UpstreamEcho>(path);

        Assert.Equal(upstreamPath, echo!.Path);
        Assert.Equal("Bearer member-token", echo.Authorization);
    }

    [Theory]
    [InlineData("/photo-api/photos/not-a-guid")]
    [InlineData("/api/items/123")]
    [InlineData("/comms-api/topics/1")]
    public async Task Guid_route_rejects_a_non_guid_id(string path)
    {
        await using var host = await ProxyHost.StartAsync("cal", specs: Specs(typed: true));

        var res = await host.Client("member-token").GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal(0, host.Upstream.Hits);
    }

    [Fact]
    public async Task Fence_is_pinned_to_the_verb_the_upstream_serves_the_sibling_on()
    {
        await using var host = await ProxyHost.StartAsync("cal", specs: Specs(typed: false));

        var res = await host.Client("member-token").DeleteAsync("/photo-api/photos/density");
        var echo = await res.Content.ReadFromJsonAsync<UpstreamEcho>();

        Assert.Equal("/photos/density", echo!.Path);
    }

    [Fact]
    public async Task Fences_stay_out_of_the_published_document()
    {
        await using var host = await ProxyHost.StartAsync("cal", specs: Specs(typed: false));

        var fence = host.Endpoints.OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/photo-api/photos/density");

        Assert.True(fence.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription);
    }

    [Fact]
    public async Task Without_specs_routes_stay_unconstrained_and_startup_warns()
    {
        var logs = new CapturingLoggerProvider();
        await using var host = await ProxyHost.StartAsync("cal", logs: logs);

        var echo = await host.Client("member-token").GetFromJsonAsync<UpstreamEcho>("/api/items/thin");

        Assert.Equal("/items/thin", echo!.Path);
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("cal-api, comms-api, contact-api", StringComparison.Ordinal));
    }

    private static Dictionary<string, JsonObject> Specs(bool typed) => typed ? Fixture.Specs("cal") : Fixture.UntypedSpecs("cal");
}
