using System.Text.Json.Nodes;
using Xunit;

namespace Lupira.Bff.OpenApi.UnitTests;

/// <summary>
/// No-behaviour-change gate for adoption: the consumer's real exposed.json and upstream specs must merge
/// into the document it has committed, minus what its own C# endpoints add.
/// </summary>
public class CommittedDocumentTests
{
    public static TheoryData<string, string, string[]> CSharpOperations => new()
    {
        { "cal", "LupiraCalBff.json", ["GET /api/contacts/{id}/context", "GET /auth/user"] },
        { "tasks", "LupiraTasksBff.json", ["GET /auth/user", "POST /auth/guest"] },
    };

    public static TheoryData<string, string, string[]> CSharpSchemas => new()
    {
        { "cal", "LupiraCalBff.json", ["ContactContextDto", "ContactGroupRefDto", "ContactRefDto", "UserInfo"] },
        { "tasks", "LupiraTasksBff.json", ["GuestExchangeRequest", "GuestSessionInfo", "UserInfo"] },
    };

    [Theory]
    [MemberData(nameof(CSharpOperations))]
    public void Operations_match_the_committed_document(string fixture, string file, string[] declaredInCSharp)
    {
        var merged = Operations(Merge(fixture));
        var committed = Operations(Fixture.Committed(fixture, file));

        Assert.Empty(merged.Keys.Except(committed.Keys, StringComparer.Ordinal));
        Assert.Equal(declaredInCSharp.Order(StringComparer.Ordinal), committed.Keys.Except(merged.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal));

        Assert.Equal(PathOrder(merged.Keys), PathOrder(committed.Keys.Intersect(merged.Keys, StringComparer.Ordinal)));
        foreach (var (key, operation) in merged)
        {
            var published = committed[key];
            Assert.True(operation["operationId"]?.GetValue<string>() == published["operationId"]?.GetValue<string>(), $"{key}: operationId");
            Assert.True(JsonNode.DeepEquals(operation["tags"], published["tags"]), $"{key}: tags");
            Assert.True(JsonNode.DeepEquals(operation["security"], published["security"]), $"{key}: security");
            Assert.Equal(Parameters(operation), Parameters(published));
        }
    }

    [Theory]
    [MemberData(nameof(CSharpSchemas))]
    public void Schemas_and_security_schemes_match_the_committed_document(string fixture, string file, string[] declaredInCSharp)
    {
        var merged = Merge(fixture);
        var committed = Fixture.Committed(fixture, file);
        var mergedSchemas = Names(merged["components"]!["schemas"]!);
        var committedSchemas = Names(committed["components"]!["schemas"]!);

        Assert.Empty(mergedSchemas.Except(committedSchemas, StringComparer.Ordinal));
        Assert.Equal(declaredInCSharp, committedSchemas.Except(mergedSchemas, StringComparer.Ordinal).Order(StringComparer.Ordinal));
        Assert.True(JsonNode.DeepEquals(merged["components"]!["securitySchemes"], committed["components"]!["securitySchemes"]));
        Assert.True(JsonNode.DeepEquals(merged["info"], committed["info"]));
        Assert.Equal(committed["openapi"]!.GetValue<string>(), merged["openapi"]!.GetValue<string>());
    }

    private static JsonObject Merge(string fixture) =>
        Fixture.Merge(fixture, fixture == "cal" ? Fixture.CalOptions() : Fixture.TasksOptions()).Document;

    private static Dictionary<string, JsonObject> Operations(JsonObject document) =>
        document["paths"]!.AsObject()
            .SelectMany(p => p.Value!.AsObject()
                .Where(v => v.Value is JsonObject o && o.ContainsKey("responses"))
                .Select(v => (Key: $"{v.Key.ToUpperInvariant()} {p.Key}", Operation: v.Value!.AsObject())))
            .ToDictionary(o => o.Key, o => o.Operation, StringComparer.Ordinal);

    private static List<string> Parameters(JsonObject operation) =>
        (operation["parameters"]?.AsArray() ?? [])
            .Select(p => $"{p!["in"]} {p["name"]}")
            .Order(StringComparer.Ordinal)
            .ToList();

    private static List<string> PathOrder(IEnumerable<string> operations) =>
        operations.Select(o => o.Split(' ', 2)[1]).Distinct(StringComparer.Ordinal).ToList();

    private static List<string> Names(JsonNode schemas) => schemas.AsObject().Select(s => s.Key).ToList();
}
