using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;

namespace Lupira.Bff.Proxy;

/// <summary>
/// Every <c>VERB /path</c> the BFF forwards. A positive list, so an endpoint an upstream grows later
/// stays invisible until someone adds a line.
/// </summary>
public sealed partial class ExposedSurface
{
    private const string ClustersKey = "clusters";
    private const string GroupsKey = "groups";

    private static readonly HashSet<string> GroupProperties =
        new(StringComparer.Ordinal) { "policy", "prefixed", "catchAll", "documented", "credential", "pathMap" };

    private ExposedSurface(
        IReadOnlyDictionary<string, ExposedCluster> clusters,
        IReadOnlyList<ExposedGroup> groups,
        IReadOnlyList<ExposedOperation> operations)
    {
        Clusters = clusters;
        Groups = groups;
        Operations = operations;
        ApiPrefixes = FirstSegments(operations);
        DeviceKeyPrefixes = FirstSegments(operations.Where(o => o.Group.Credential == UpstreamCredential.DeviceKey));
    }

    public IReadOnlyDictionary<string, ExposedCluster> Clusters { get; }

    public IReadOnlyList<ExposedGroup> Groups { get; }

    public IReadOnlyList<ExposedOperation> Operations { get; }

    /// <summary>The first segment of every BFF path: XHR/native surfaces that want status codes, never redirects.</summary>
    public IReadOnlyList<string> ApiPrefixes { get; }

    public IReadOnlyList<string> DeviceKeyPrefixes { get; }

    public IEnumerable<ExposedOperation> Documented => Operations.Where(o => o.Group.Documented);

    public static ExposedSurface Load(IHostEnvironment environment) =>
        Load(Assembly.Load(new AssemblyName(environment.ApplicationName)));

    public static ExposedSurface Load(Assembly assembly)
    {
        var names = assembly.GetManifestResourceNames()
            .Where(n => n == "exposed.json" || n.EndsWith(".exposed.json", StringComparison.Ordinal))
            .ToList();
        if (names.Count != 1)
        {
            throw new InvalidOperationException(
                $"{assembly.GetName().Name} must embed exactly one exposed.json (found {names.Count}).");
        }

        using var stream = assembly.GetManifestResourceStream(names[0])!;
        return Parse(JsonNode.Parse(stream));
    }

    public static ExposedSurface Parse(string json) => Parse(JsonNode.Parse(json));

    private static ExposedSurface Parse(JsonNode? node)
    {
        var root = node as JsonObject ?? throw new InvalidOperationException("exposed.json is not a JSON object.");

        var clusters = new Dictionary<string, ExposedCluster>(StringComparer.Ordinal);
        foreach (var (name, value) in Object(root[ClustersKey], ClustersKey))
        {
            var cluster = Object(value, $"{ClustersKey}.{name}");
            clusters[name] = new ExposedCluster
            {
                Prefix = String(cluster["prefix"], $"{ClustersKey}.{name}.prefix")
                    ?? throw new InvalidOperationException($"Cluster {name} has no prefix."),
                AnnouncesPrefix = Bool(cluster["announcePrefix"], $"{ClustersKey}.{name}.announcePrefix") ?? false,
            };
        }

        var definitions = Object(root[GroupsKey], GroupsKey);
        var groups = new List<ExposedGroup>();
        var operations = new List<ExposedOperation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (name, entries) in root)
        {
            if (name is ClustersKey or GroupsKey) continue;

            var group = definitions[name] is { } definition
                ? ReadGroup(name, Object(definition, $"{GroupsKey}.{name}"))
                : name == ExposedGroup.DefaultName
                    ? new ExposedGroup { Name = name, Documented = true }
                    : throw new InvalidOperationException($"exposed.json group '{name}' is not declared under '{GroupsKey}'.");
            groups.Add(group);

            foreach (var (cluster, list) in Object(entries, name))
            {
                var prefix = group.Prefixed
                    ? clusters.TryGetValue(cluster, out var mount)
                        ? mount.Prefix
                        : throw new InvalidOperationException($"No BFF prefix for cluster {cluster}.")
                    : string.Empty;

                foreach (var item in list as JsonArray ?? throw new InvalidOperationException($"{name}.{cluster} is not an array."))
                {
                    var entry = String(item, $"{name}.{cluster}[]")!;
                    var (verb, path) = Split(entry);
                    Validate(group, cluster, entry, verb, path);
                    if (!seen.Add($"{name} {cluster} {entry}"))
                        throw new InvalidOperationException($"exposed.json lists '{entry}' twice in {name}.{cluster}.");

                    var mapped = group.PathMap is { } map ? map.ToBff(path) : path;
                    operations.Add(new ExposedOperation
                    {
                        Group = group,
                        Cluster = cluster,
                        Verb = verb,
                        UpstreamPath = path,
                        MappedPath = mapped,
                        BffPath = prefix + mapped,
                    });
                }
            }
        }

        foreach (var (name, _) in definitions)
        {
            if (!root.ContainsKey(name))
                throw new InvalidOperationException($"exposed.json declares group '{name}' but lists no entries for it.");
        }

        return new ExposedSurface(clusters, groups, operations);
    }

    private static ExposedGroup ReadGroup(string name, JsonObject definition)
    {
        var at = $"{GroupsKey}.{name}";
        foreach (var (property, _) in definition)
        {
            if (!GroupProperties.Contains(property))
                throw new InvalidOperationException($"Unknown property '{property}' in {at}.");
        }

        var group = new ExposedGroup
        {
            Name = name,
            Policy = String(definition["policy"], $"{at}.policy") ?? "Default",
            Prefixed = Bool(definition["prefixed"], $"{at}.prefixed") ?? true,
            CatchAll = Bool(definition["catchAll"], $"{at}.catchAll") ?? false,
            Documented = Bool(definition["documented"], $"{at}.documented") ?? false,
            Credential = String(definition["credential"], $"{at}.credential") switch
            {
                null or "session" => UpstreamCredential.Session,
                "deviceKey" => UpstreamCredential.DeviceKey,
                "none" => UpstreamCredential.None,
                var other => throw new InvalidOperationException($"{at}.credential '{other}' is not session, deviceKey or none."),
            },
            PathMap = definition["pathMap"] is { } map ? ReadPathMap(Object(map, $"{at}.pathMap"), $"{at}.pathMap") : null,
        };

        if (group.CatchAll && group.Documented)
            throw new InvalidOperationException($"{at}: a catch-all group cannot be documented.");
        return group;
    }

    private static ExposedPathMap ReadPathMap(JsonObject map, string at)
    {
        var upstream = String(map["upstream"], $"{at}.upstream") ?? throw new InvalidOperationException($"{at}.upstream is required.");
        var bff = String(map["bff"], $"{at}.bff") ?? throw new InvalidOperationException($"{at}.bff is required.");
        var claims = Object(map["claims"], $"{at}.claims")
            .ToDictionary(c => c.Key, c => String(c.Value, $"{at}.claims.{c.Key}")!, StringComparer.Ordinal);

        var parameters = RouteParameter().Matches(upstream).Select(m => m.Groups[1].Value).ToList();
        var unfilled = parameters.Except(claims.Keys, StringComparer.Ordinal).ToList();
        if (unfilled.Count > 0)
            throw new InvalidOperationException($"{at}: '{string.Join("', '", unfilled)}' has no claim to refill it from.");
        if (claims.Keys.Except(parameters, StringComparer.Ordinal).FirstOrDefault() is { } stray)
            throw new InvalidOperationException($"{at}.claims names '{stray}', which is not a parameter of {upstream}.");

        return new ExposedPathMap { Upstream = upstream, Bff = bff, Claims = claims };
    }

    private static void Validate(ExposedGroup group, string cluster, string entry, string verb, string path)
    {
        var at = $"{group.Name}.{cluster} '{entry}'";
        if (!path.StartsWith('/'))
            throw new InvalidOperationException($"{at}: the path must start with '/'.");

        var catchAll = path.Contains("{*", StringComparison.Ordinal);
        if (group.CatchAll)
        {
            if (verb != "GET")
                throw new InvalidOperationException($"{at}: a catch-all group is GET-only.");
            if (!TrailingCatchAll().IsMatch(path))
                throw new InvalidOperationException($"{at}: a catch-all entry must end in /{{**name}}.");
        }
        else if (catchAll)
        {
            throw new InvalidOperationException($"{at}: a catch-all forwards whatever the upstream adds under it; declare a catch-all group.");
        }

        if (group.PathMap is { } map && !map.Covers(path))
            throw new InvalidOperationException($"{at}: outside the group's path map {map.Upstream}.");
    }

    private static (string Verb, string Path) Split(string entry)
    {
        var parts = entry.Split(' ', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && parts[0].Length > 0 && parts[0].All(char.IsAsciiLetterUpper)
            ? (parts[0], parts[1])
            : throw new InvalidOperationException($"exposed.json entry '{entry}' is not 'VERB /path'.");
    }

    private static IReadOnlyList<string> FirstSegments(IEnumerable<ExposedOperation> operations) =>
        operations.Select(o => $"/{o.BffPath.TrimStart('/').Split('/')[0]}").Distinct(StringComparer.Ordinal).ToList();

    private static JsonObject Object(JsonNode? node, string at) => node switch
    {
        null => [],
        JsonObject obj => obj,
        _ => throw new InvalidOperationException($"exposed.json '{at}' is not an object."),
    };

    private static string? String(JsonNode? node, string at) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue<string>(out var s) => s,
        _ => throw new InvalidOperationException($"exposed.json '{at}' is not a string."),
    };

    private static bool? Bool(JsonNode? node, string at) => node switch
    {
        null => null,
        JsonValue value when value.TryGetValue<bool>(out var b) => b,
        _ => throw new InvalidOperationException($"exposed.json '{at}' is not a boolean."),
    };

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex RouteParameter();

    [GeneratedRegex(@"/\{\*\*\w+\}$")]
    private static partial Regex TrailingCatchAll();
}
