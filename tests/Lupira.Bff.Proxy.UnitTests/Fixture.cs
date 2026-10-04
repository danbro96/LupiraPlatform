using System.Text.Json.Nodes;

namespace Lupira.Bff.Proxy.UnitTests;

internal static class Fixture
{
    private static readonly Dictionary<string, (string Cluster, string Name)[]> Upstreams = new(StringComparer.Ordinal)
    {
        ["cal"] =
        [
            ("cal-api", "LupiraCalApi"),
            ("contact-api", "LupiraContactApi"),
            ("geo-api", "LupiraGeoApi"),
            ("tasks-api", "LupiraTasksApi"),
            ("location-api", "LupiraLocationApi"),
            ("photo-api", "LupiraPhotoApi"),
            ("comms-api", "LupiraCommsApi"),
        ],
        ["tasks"] = [("tasks-api", "LupiraTasksApi")],
    };

    public static string Json(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"{name}.exposed.json"));

    public static ExposedSurface Surface(string name) => ExposedSurface.Parse(Json(name));

    public static Dictionary<string, JsonObject> Specs(string name) =>
        Upstreams[name].ToDictionary(
            u => u.Cluster,
            u => JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name, "upstream", $"{u.Name}.json")))!.AsObject(),
            StringComparer.Ordinal);

    /// <summary>Path parameters lose their format, so the fences are the only guard left.</summary>
    public static Dictionary<string, JsonObject> UntypedSpecs(string name)
    {
        var specs = Specs(name);
        foreach (var item in specs.Values.SelectMany(s => s["paths"]!.AsObject()).Select(p => p.Value!.AsObject()))
        {
            foreach (var holder in item.Select(kv => kv.Value).OfType<JsonObject>().Prepend(item))
            {
                foreach (var parameter in (holder["parameters"] as JsonArray ?? []).OfType<JsonObject>())
                    (parameter["schema"] as JsonObject)?.Remove("format");
            }
        }

        return specs;
    }
}
