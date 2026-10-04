using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lupira.Bff.Proxy.UnitTests;

/// <summary>
/// The route table is computed from <c>exposed.json</c> at startup: the allowlist and the route table
/// must agree, and the keys must be shaped the way YARP's <c>LoadFromConfig</c> reads them.
/// </summary>
public class ProxyRoutesTests
{
    public static TheoryData<string> Fixtures => new() { "cal", "tasks", "assistant" };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Every_allowlisted_operation_is_routed_exactly_once(string fixture)
    {
        var surface = Fixture.Surface(fixture);
        var routed = Routes(surface).GetChildren()
            .SelectMany(route => route.GetSection("Match:Methods").GetChildren()
                .Select(m => $"{m.Value} {route["Match:Path"]}"))
            .ToList();

        var declared = surface.Operations.Select(o => $"{o.Verb} {o.BffPath}").ToList();

        Assert.Equal(declared.Count, routed.Count);
        Assert.Empty(declared.Except(routed, StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Route_keys_are_unique(string fixture)
    {
        var surface = Fixture.Surface(fixture);
        var keys = ProxyRoutes.Plan(surface).Select(r => r.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(keys.Count, Routes(surface).GetChildren().Count());
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Only_catch_all_groups_are_wildcards_and_they_are_get_only(string fixture)
    {
        var surface = Fixture.Surface(fixture);
        foreach (var route in ProxyRoutes.Plan(surface))
        {
            // Anything else forwards whatever the upstream adds under it, unreviewed.
            Assert.Equal(route.Group.CatchAll, route.Path.Contains("**", StringComparison.Ordinal));
            if (route.Group.CatchAll) Assert.Equal(["GET"], route.Verbs);
            Assert.NotEmpty(route.Verbs);
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Prefixed_routes_strip_their_prefix_and_unprefixed_ones_are_untransformed(string fixture)
    {
        var surface = Fixture.Surface(fixture);
        var routes = Routes(surface);
        foreach (var route in ProxyRoutes.Plan(surface))
        {
            var section = routes.GetSection(route.Key);
            Assert.Equal(route.Group.Policy, section["AuthorizationPolicy"]);
            if (route.Group.Prefixed)
            {
                var prefix = surface.Clusters[route.Cluster].Prefix;
                Assert.Equal(prefix, section["Transforms:0:PathRemovePrefix"]);
                Assert.StartsWith(prefix + "/", route.Path + "/", StringComparison.Ordinal);
            }
            else
            {
                Assert.Empty(section.GetSection("Transforms").GetChildren());
            }
        }
    }

    [Fact]
    public void Cal_device_ingest_is_anonymous_unprefixed_and_keyed_by_verb()
    {
        var device = ProxyRoutes.Plan(Fixture.Surface("cal")).Where(r => r.Group.Name == "device").ToList();

        Assert.Equal(
            ["location-api-ingest-location-POST-device", "location-api-ingest-location-cursor-GET-device", "location-api-ingest-location-state-GET-device"],
            device.Select(r => r.Key).Order(StringComparer.Ordinal));
        Assert.All(device, r =>
        {
            Assert.Equal("Anonymous", r.Group.Policy);
            Assert.Null(r.RemovePrefix);
            Assert.StartsWith("/ingest/", r.Path, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Cal_routes_nothing_that_uses_a_different_credential()
    {
        // Anchored to the resource root, so a list owner's own /lists/{id}/shares stays fine. Device
        // ingest sits at /ingest/location with no cluster prefix, so it does not match either.
        var forbidden = new Regex(@"^/[a-z-]+/(pingz|ingest|shared|shares|users|mcp|internal|dav-backend|openapi|scalar|depz)(/|$)");

        var offending = ProxyRoutes.Plan(Fixture.Surface("cal")).Select(r => r.Path).Where(p => forbidden.IsMatch(p)).ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Tasks_routes_nothing_behind_the_dav_seam_mcp_or_the_probe_and_doc_endpoints()
    {
        var forbidden = new Regex(@"^/api/(dav-backend|mcp|\.well-known|pingz|livez|readyz|openapi|scalar)(/|$)");

        var offending = ProxyRoutes.Plan(Fixture.Surface("tasks")).Select(r => r.Path).Where(p => forbidden.IsMatch(p)).ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Tasks_share_surface_alone_gets_the_guest_policy_and_drops_the_token_segment()
    {
        // POST /shares/redeem is member-authed and one letter from the account-less /share surface; a
        // prefix rule instead of exact templates would silently downgrade its auth.
        var byPath = ProxyRoutes.Plan(Fixture.Surface("tasks")).ToDictionary(r => r.Path, r => r.Group.Policy);

        foreach (var (path, policy) in byPath)
        {
            var isShare = path == "/api/share" || path.StartsWith("/api/share/", StringComparison.Ordinal);
            Assert.Equal(isShare ? "Guest" : "Default", policy);
            Assert.DoesNotContain("{token}", path, StringComparison.Ordinal);
        }

        Assert.Equal("Default", byPath["/api/shares/redeem"]);
        Assert.Contains("/api/share/items/{itemId}", byPath.Keys);
    }

    [Fact]
    public void Assistant_only_the_enrollment_legs_and_device_ingest_are_anonymous()
    {
        var anonymous = ProxyRoutes.Plan(Fixture.Surface("assistant"))
            .Where(r => r.Group.Policy == "Anonymous")
            .Select(r => r.Path)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            ["/api/auth/callback", "/api/auth/done", "/api/auth/login",
             "/ingest/location", "/ingest/location/cursor", "/ingest/location/state",
             "/ingest/ring", "/ingest/summaries"],
            anonymous);
    }

    [Fact]
    public void Assistant_only_assistant_api_announces_its_prefix()
    {
        var surface = Fixture.Surface("assistant");
        var routes = Routes(surface);
        foreach (var route in ProxyRoutes.Plan(surface).Where(r => r.Group.Prefixed))
        {
            var section = routes.GetSection(route.Key);
            var prefix = surface.Clusters[route.Cluster].Prefix;
            Assert.Equal(route.Cluster == "assistant-api" ? prefix : null, section["Transforms:2:Set"]);
            Assert.Equal(route.Cluster == "assistant-api" ? "X-Forwarded-Prefix" : null, section["Transforms:2:RequestHeader"]);
        }
    }

    [Theory]
    [InlineData("operations", "", """{ "cal-api": ["GET /items/{id}", "GET /items-id"] }""")]
    [InlineData("static", "\"static\": { \"catchAll\": true }", """{ "cal-api": ["GET /basemap/{**path}", "GET /basemap-/{**rest}"] }""")]
    [InlineData("device", "\"device\": { \"policy\": \"Anonymous\", \"prefixed\": false, \"credential\": \"deviceKey\" }", """{ "cal-api": ["POST /ingest/location", "POST /ingest-location"] }""")]
    public void A_colliding_allowlist_throws_rather_than_dropping_a_route(string group, string declaration, string entries)
    {
        // The key is derived from cluster + path with punctuation flattened, so two paths could collide
        // and one would silently win.
        var surface = ExposedSurface.Parse($$"""
            {
              "clusters": { "cal-api": { "prefix": "/api" } },
              "groups": { {{declaration}} },
              "{{group}}": {{entries}}
            }
            """);

        var error = Assert.Throws<InvalidOperationException>(() => ProxyRoutes.Build(surface));
        Assert.Contains("collision", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Reads the generated keys back exactly as the app does.</summary>
    private static IConfigurationSection Routes(ExposedSurface surface) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(ProxyRoutes.Build(surface))
            .Build()
            .GetSection("ReverseProxy:Routes");
}
