using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

namespace Lupira.Bff.Proxy.UnitTests;

public class RouteGuardsTests
{
    public static TheoryData<string> Fixtures => new() { "cal", "tasks" };

    [Fact]
    public void Cal_fences_exactly_the_unlisted_siblings_its_templates_would_capture()
    {
        var plan = RouteGuards.Plan(Fixture.Surface("cal"), Fixture.Specs("cal"));

        Assert.Equal(
            ["GET /api/items/thin", "GET /contact-api/contacts/birthdays", "GET /contact-api/contacts/thin",
             "GET /geo-api/places/duplicates", "GET /photo-api/photos/density"],
            plan.Fences.Select(f => $"{f.Verb} {f.Path}"));
        Assert.Empty(plan.UnguardedClusters);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Guarded_routes_only_gain_constraints_and_no_fence_shadows_a_listed_operation(string fixture)
    {
        var surface = Fixture.Surface(fixture);
        var plan = RouteGuards.Plan(surface, Fixture.Specs(fixture));
        var routes = ProxyRoutes.Plan(surface).ToDictionary(r => r.Key, StringComparer.Ordinal);

        Assert.NotEmpty(plan.Paths);
        foreach (var (key, path) in plan.Paths)
            Assert.Equal(routes[key].Path, Regex.Replace(path, @":\w+\}", "}"));

        var listed = surface.Operations.Select(o => $"{o.Verb} {o.BffPath}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(plan.Fences, f => listed.Contains($"{f.Verb} {f.Path}"));
    }

    [Fact]
    public void Uuid_parameters_become_guid_constraints()
    {
        var plan = RouteGuards.Plan(Fixture.Surface("cal"), Fixture.Specs("cal"));

        Assert.Equal("/photo-api/photos/{id:guid}", plan.Paths["photo-api-photos-id"]);
        Assert.Equal("/api/items/{itemId:guid}/calendars/{calendarId:guid}", plan.Paths["cal-api-items-itemId-calendars-calendarId"]);
        Assert.False(plan.Paths.ContainsKey("photo-api-photos-map"));
    }

    [Fact]
    public void A_cluster_without_a_spec_is_reported_and_left_unguarded()
    {
        var specs = Fixture.Specs("cal");
        specs.Remove("geo-api");

        var plan = RouteGuards.Plan(Fixture.Surface("cal"), specs);

        Assert.Equal(["geo-api"], plan.UnguardedClusters);
        Assert.DoesNotContain(plan.Paths.Keys, k => k.StartsWith("geo-api-", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Fences, f => f.Cluster == "geo-api");
    }

    [Fact]
    public void Constraints_follow_the_schema_and_fences_follow_upstream_precedence_per_verb()
    {
        var surface = ExposedSurface.Parse("""
            {
              "clusters": { "up": { "prefix": "/p" } },
              "groups": {
                "static": { "catchAll": true },
                "guest": { "policy": "Guest", "credential": "none", "pathMap": { "upstream": "/shared/{token}", "bff": "/share", "claims": { "token": "share-token" } } }
              },
              "operations": { "up": ["GET /things/{id}", "DELETE /things/{id}", "GET /things/{id}/flag/{on}", "GET /things/listed", "GET /a/{x}/c", "GET /names/{name}"] },
              "guest": { "up": ["GET /shared/{token}/items/{itemId}"] },
              "static": { "up": ["GET /files/{**path}"] }
            }
            """);
        var spec = JsonNode.Parse("""
            {
              "paths": {
                "/things/{id}": {
                  "get": { "parameters": [{ "in": "path", "name": "id", "schema": { "type": "integer", "format": "int32" } }], "responses": {} },
                  "delete": { "parameters": [{ "in": "path", "name": "id", "schema": { "type": "integer", "format": "int64" } }], "responses": {} }
                },
                "/things/{id}/flag/{on}": {
                  "parameters": [{ "in": "path", "name": "id", "schema": { "type": "integer", "format": "int64" } }],
                  "get": { "parameters": [{ "in": "path", "name": "on", "schema": { "type": "boolean" } }], "responses": {} }
                },
                "/things/hidden": { "get": { "responses": {} } },
                "/things/listed": { "get": { "responses": {} } },
                "/things/postonly": { "post": { "responses": {} } },
                "/a/{x}/c": { "get": { "responses": {} } },
                "/a/b/{y}": { "get": { "responses": {} } },
                "/names/{name}": { "get": { "parameters": [{ "in": "path", "name": "name", "schema": { "type": "string" } }], "responses": {} } },
                "/shared/{token}/items/{itemId}": { "get": { "parameters": [{ "in": "path", "name": "itemId", "schema": { "type": "string", "format": "uuid" } }], "responses": {} } },
                "/shared/{token}/items/secret": { "get": { "responses": {} } },
                "/files/admin": { "get": { "responses": {} } }
              }
            }
            """)!.AsObject();

        var plan = RouteGuards.Plan(surface, new Dictionary<string, JsonObject> { ["up"] = spec });

        Assert.Equal(
            ["/p/share/items/{itemId:guid}", "/p/things/{id:long}/flag/{on:bool}"],
            plan.Paths.Values.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["GET /p/a/b/{y}", "GET /p/files/admin", "GET /p/share/items/secret", "GET /p/things/hidden"],
            plan.Fences.Select(f => $"{f.Verb} {f.Path}"));
    }
}
