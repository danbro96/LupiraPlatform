using System.Text.Json.Nodes;
using Lupira.Bff.Proxy;

namespace Lupira.Bff.OpenApi.UnitTests;

internal static class Fixture
{
    private static readonly (string Cluster, string Name)[] CalSources =
    [
        ("cal-api", "LupiraCalApi"),
        ("contact-api", "LupiraContactApi"),
        ("geo-api", "LupiraGeoApi"),
        ("tasks-api", "LupiraTasksApi"),
        ("location-api", "LupiraLocationApi"),
        ("photo-api", "LupiraPhotoApi"),
        ("comms-api", "LupiraCommsApi"),
    ];

    public static ExposedSurface Surface(string name) => ExposedSurface.Parse(Read($"{name}.exposed.json").ToJsonString());

    public static JsonObject Committed(string name, string file) => Read(name, file);

    public static UpstreamSpecMergerOptions CalOptions()
    {
        var options = new UpstreamSpecMergerOptions
        {
            Title = "LupiraCal BFF",
            Version = "v1",
            RetagByCluster = true,
            NamespaceCollisions = true,
        };
        foreach (var (cluster, name) in CalSources)
            options.Upstreams.Add(new UpstreamSpec { Cluster = cluster, Name = name });
        options.SecuritySchemes["Cookie"] = BffSecuritySchemes.Cookie("__Host-lupira-cal", "Session cookie minted by the BFF's OIDC login.");
        options.SecuritySchemes["Bearer"] = BffSecuritySchemes.Bearer("Authentik access token from a native client; audience must include lupira-cal.");
        return options;
    }

    public static UpstreamSpecMergerOptions TasksOptions()
    {
        var options = new UpstreamSpecMergerOptions
        {
            Title = "LupiraTasks BFF",
            SortPaths = true,
            SecurityFor = operation => operation.Group.Name == "guest" ? ["GuestCookie"] : ["Cookie", "Bearer"],
        };
        options.Upstreams.Add(new UpstreamSpec { Cluster = "tasks-api", Name = "LupiraTasksApi" });
        options.SecuritySchemes["Cookie"] = BffSecuritySchemes.Cookie("__Host-lupira-tasks", "Member session cookie minted by the BFF's OIDC login.");
        options.SecuritySchemes["Bearer"] = BffSecuritySchemes.Bearer("Authentik access token from the mobile app; audience must include lupira-tasks.");
        options.SecuritySchemes["GuestCookie"] = BffSecuritySchemes.Cookie("__Host-lupira-tasks-guest", "Account-less share session, minted by POST /auth/guest from a share token.");
        return options;
    }

    public static MergeResult Merge(string name, UpstreamSpecMergerOptions options) =>
        UpstreamSpecMerger.Merge(Surface(name), Upstreams(name, options), options);

    public static IReadOnlyList<UpstreamDocument> Upstreams(string name, UpstreamSpecMergerOptions options) =>
        options.Upstreams
            .Select(u => new UpstreamDocument { Cluster = u.Cluster, Document = Read(name, "upstream", $"{u.Name}.json") })
            .ToList();

    private static JsonObject Read(params string[] path) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Fixtures", .. path])))!.AsObject();
}
