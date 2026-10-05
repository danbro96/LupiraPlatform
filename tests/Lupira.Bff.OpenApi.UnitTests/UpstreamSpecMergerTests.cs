using System.Text;
using System.Text.Json.Nodes;
using Lupira.Bff.Proxy;
using Microsoft.OpenApi;
using Xunit;

namespace Lupira.Bff.OpenApi.UnitTests;

/// <summary>
/// The merged document is what both the client and Scalar read, so the allowlist has to be the only
/// thing that decides what appears in it — and every operation has to carry a resolvable credential.
/// </summary>
public class UpstreamSpecMergerTests
{
    private static readonly MergeResult Cal = Fixture.Merge("cal", Fixture.CalOptions());
    private static readonly MergeResult Tasks = Fixture.Merge("tasks", Fixture.TasksOptions());

    public static TheoryData<string> Fixtures => new() { "cal", "tasks" };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Every_operation_declares_a_scheme_the_document_defines(string fixture)
    {
        var merged = Merged(fixture);
        var declared = merged.Document["components"]!["securitySchemes"]!.AsObject()
            .Select(s => s.Key).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(declared);
        foreach (var (_, _, operation) in Operations(merged))
        {
            var requirements = operation["security"]?.AsArray();
            Assert.NotNull(requirements);

            // An empty requirement object reads as "callable unauthenticated" — it is what a dangling
            // scheme reference degrades to on write.
            Assert.NotEmpty(requirements!);
            foreach (var requirement in requirements!)
            {
                var named = requirement!.AsObject().Select(r => r.Key).ToList();
                Assert.NotEmpty(named);
                Assert.All(named, name => Assert.Contains(name, declared));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Only_documented_allowlisted_operations_appear(string fixture)
    {
        var allowed = Fixture.Surface(fixture).Documented
            .Select(o => $"{o.Verb} {o.BffPath}")
            .ToHashSet(StringComparer.Ordinal);

        var present = Operations(Merged(fixture)).Select(o => $"{o.Verb.ToUpperInvariant()} {o.Path}").ToList();

        Assert.Equal(allowed.Count, present.Count);
        Assert.Empty(present.Except(allowed, StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Every_referenced_schema_is_defined(string fixture)
    {
        var merged = Merged(fixture);
        var defined = merged.Document["components"]!["schemas"]!.AsObject()
            .Select(s => s.Key).ToHashSet(StringComparer.Ordinal);

        var referenced = new List<string>();
        Collect(merged.Document, referenced);

        Assert.Empty(referenced.Distinct(StringComparer.Ordinal).Except(defined, StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task The_document_loads_without_diagnostics(string fixture)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Merged(fixture).Document.ToJsonString()));

        var read = await OpenApiDocument.LoadAsync(stream, "json");

        Assert.Empty(read.Diagnostic?.Errors ?? []);
    }

    [Fact]
    public void Cal_device_and_static_surfaces_stay_out_of_the_document()
    {
        // The generated client can only express Bearer, which is why the uploader is hand-written.
        var undocumented = Fixture.Surface("cal").Operations
            .Where(o => o.Group.Name is "device" or "static")
            .Select(o => o.BffPath)
            .ToList();

        Assert.NotEmpty(undocumented);
        Assert.Empty(Operations(Cal).Select(o => o.Path).Intersect(undocumented, StringComparer.Ordinal));
        Assert.DoesNotContain(Cal.NotExposed, entry => entry.Contains("/ingest/location", StringComparison.Ordinal));
    }

    [Fact]
    public void Cal_retags_by_cluster_and_namespaces_colliding_operation_ids()
    {
        Assert.All(Operations(Cal), o =>
            Assert.Equal(o.Path.StartsWith("/api/", StringComparison.Ordinal) ? "cal" : o.Path.Split('/')[1].Replace("-api", string.Empty, StringComparison.Ordinal), Assert.Single(o.Operation["tags"]!.AsArray())!.GetValue<string>()));

        var ids = Operations(Cal).Select(o => o.Operation["operationId"]!.GetValue<string>()).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("GetItem() (cal-api vs tasks-api) -> TasksGetItem()", Cal.Renames);
    }

    [Fact]
    public void Tasks_keeps_upstream_tags_and_remounts_the_share_surface_without_its_token()
    {
        var share = Operations(Tasks).Where(o => o.Path == "/api/share" || o.Path.StartsWith("/api/share/", StringComparison.Ordinal)).ToList();

        Assert.Equal(7, share.Count);
        Assert.All(share, o =>
        {
            Assert.Equal("Shared", Assert.Single(o.Operation["tags"]!.AsArray())!.GetValue<string>());
            Assert.Equal("GuestCookie", Assert.Single(Assert.Single(o.Operation["security"]!.AsArray())!.AsObject()).Key);
            var names = o.Operation["parameters"]?.AsArray().Select(p => p!["name"]!.GetValue<string>()) ?? [];
            Assert.DoesNotContain("token", names);
            Assert.DoesNotContain("{token}", o.Path, StringComparison.Ordinal);
        });
        Assert.Equal("v1", Tasks.Document["info"]!["version"]!.GetValue<string>());
    }

    [Fact]
    public void A_listed_operation_the_upstream_lacks_throws()
    {
        var surface = ExposedSurface.Parse("""
            { "clusters": { "tasks-api": { "prefix": "/api" } }, "operations": { "tasks-api": ["GET /lists", "GET /gone"] } }
            """);
        var options = Fixture.TasksOptions();

        var error = Assert.Throws<InvalidOperationException>(() =>
            UpstreamSpecMerger.Merge(surface, Fixture.Upstreams("tasks", options), options));
        Assert.Contains("GET /gone", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_collision_throws_unless_namespacing_is_on()
    {
        var options = Fixture.CalOptions();
        options.NamespaceCollisions = false;

        var error = Assert.Throws<InvalidOperationException>(() => Fixture.Merge("cal", options));
        Assert.Contains("NamespaceCollisions", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_schema_pointing_at_a_namespaced_schema_is_namespaced_too()
    {
        var merged = MergePages(calItemField: "title", tasksItemField: "listId");
        var schemas = merged.Document["components"]!["schemas"]!.AsObject();

        Assert.Equal("#/components/schemas/Item", ChangedRef(schemas["Page"]!));
        Assert.Equal("#/components/schemas/TasksItem", ChangedRef(schemas["TasksPage"]!));
        Assert.Contains("Page (cal-api vs tasks-api) -> TasksPage", merged.Renames);
        var tasksResponse = merged.Document["paths"]!["/tasks-api/sync/items"]!["get"]!["responses"]!["200"]!;
        Assert.Equal("#/components/schemas/TasksPage", tasksResponse["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
    }

    [Fact]
    public void Schemas_with_identical_referents_still_dedupe()
    {
        var merged = MergePages(calItemField: "title", tasksItemField: "title");

        Assert.DoesNotContain(merged.Renames, r => !r.EndsWith("()", StringComparison.Ordinal));
        Assert.Equal(["Item", "Page"], merged.Document["components"]!["schemas"]!.AsObject().Select(s => s.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_scheme_the_document_does_not_define_throws()
    {
        var options = Fixture.TasksOptions();
        options.SecuritySchemes.Remove("GuestCookie");

        var error = Assert.Throws<InvalidOperationException>(() => Fixture.Merge("tasks", options));
        Assert.Contains("GuestCookie", error.Message, StringComparison.Ordinal);
    }

    private static MergeResult Merged(string fixture) => fixture == "cal" ? Cal : Tasks;

    private static MergeResult MergePages(string calItemField, string tasksItemField)
    {
        var surface = ExposedSurface.Parse("""
            {
              "clusters": { "cal-api": { "prefix": "/api" }, "tasks-api": { "prefix": "/tasks-api" } },
              "operations": { "cal-api": ["GET /sync/items"], "tasks-api": ["GET /sync/items"] }
            }
            """);
        var options = new UpstreamSpecMergerOptions { NamespaceCollisions = true, SecurityFor = _ => ["Bearer"] };
        options.SecuritySchemes["Bearer"] = BffSecuritySchemes.Bearer("Access token.");
        return UpstreamSpecMerger.Merge(surface, [PageUpstream("cal-api", calItemField), PageUpstream("tasks-api", tasksItemField)], options);
    }

    private static UpstreamDocument PageUpstream(string cluster, string itemField) => new()
    {
        Cluster = cluster,
        Document = JsonNode.Parse($$"""
            {
              "openapi": "3.1.1",
              "info": { "title": "{{cluster}}", "version": "v1" },
              "paths": {
                "/sync/items": {
                  "get": {
                    "operationId": "SyncItems",
                    "responses": { "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Page" } } } } }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Page": { "type": "object", "properties": { "changed": { "type": "array", "items": { "$ref": "#/components/schemas/Item" } } } },
                  "Item": { "type": "object", "properties": { "{{itemField}}": { "type": "string" } } }
                }
              }
            }
            """)!.AsObject(),
    };

    private static string ChangedRef(JsonNode page) => page["properties"]!["changed"]!["items"]!["$ref"]!.GetValue<string>();

    private static IEnumerable<(string Path, string Verb, JsonObject Operation)> Operations(MergeResult merged)
    {
        foreach (var (path, item) in merged.Document["paths"]!.AsObject())
        {
            foreach (var (verb, node) in item!.AsObject())
            {
                if (node is JsonObject operation && operation.ContainsKey("responses"))
                    yield return (path, verb, operation);
            }
        }
    }

    private static void Collect(JsonNode node, List<string> refs)
    {
        const string prefix = "#/components/schemas/";
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj)
                {
                    if (key == "$ref" && value?.GetValue<string>() is { } reference
                        && reference.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        refs.Add(reference[prefix.Length..]);
                    }
                    else if (value is not null)
                    {
                        Collect(value, refs);
                    }
                }

                break;
            case JsonArray array:
                foreach (var value in array)
                {
                    if (value is not null) Collect(value, refs);
                }

                break;
        }
    }
}
