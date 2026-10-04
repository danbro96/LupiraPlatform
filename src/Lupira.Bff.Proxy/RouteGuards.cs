using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Lupira.Bff.Proxy;

/// <summary>
/// Narrows each templated route to what the upstream would route to that operation. A bare <c>{id}</c>
/// matches any segment, so <c>/items/{id}</c> would forward <c>/items/thin</c> — an unlisted upstream
/// operation — with the member's token.
/// </summary>
/// <remarks>
/// Two guards from the upstream spec: a type constraint per parameter schema, and a 404 fence per
/// unlisted upstream operation that outranks a listed template under ASP.NET route precedence. The
/// fences mirror the upstream's own routing, so they stay exact per verb and never touch a route key.
/// </remarks>
public static partial class RouteGuards
{
    private static readonly HashSet<string> HttpVerbs =
        new(StringComparer.Ordinal) { "get", "put", "post", "delete", "patch", "head", "options", "trace" };

    private enum SegmentKind
    {
        Literal,
        Complex,
        Parameter,
        CatchAll,
    }

    public static RouteGuardPlan Plan(ExposedSurface surface, IReadOnlyDictionary<string, JsonObject> specs)
    {
        var upstreams = specs.ToDictionary(s => s.Key, s => Read(s.Value), StringComparer.Ordinal);
        var listed = surface.Operations.Select(o => $"{o.Verb} {o.BffPath}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        var fences = new List<RouteFence>();
        var fenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unguarded = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var route in ProxyRoutes.Plan(surface))
        {
            if (!upstreams.TryGetValue(route.Cluster, out var upstream))
            {
                unguarded.Add(route.Cluster);
                continue;
            }

            var operations = surface.Operations
                .Where(o => o.Group == route.Group && o.Cluster == route.Cluster && o.BffPath == route.Path && route.Verbs.Contains(o.Verb))
                .ToList();

            var declared = operations.Select(o => upstream.FirstOrDefault(u => u.Verb == o.Verb && u.Path == o.UpstreamPath)).ToList();
            if (declared.All(d => d is not null))
            {
                var typed = RouteParameter().Replace(route.Path, m => Typed(m, declared!));
                if (typed != route.Path) paths[route.Key] = typed;
            }

            foreach (var operation in operations)
            {
                var own = Segments(operation.UpstreamPath);
                foreach (var sibling in upstream.Where(u => u.Verb == operation.Verb && Outranks(u.Segments, own)))
                {
                    if (Mount(route, sibling.Path) is not { } path) continue;
                    var entry = $"{operation.Verb} {path}";
                    if (listed.Contains(entry) || !fenced.Add(entry)) continue;

                    fences.Add(new RouteFence
                    {
                        Cluster = route.Cluster,
                        Verb = operation.Verb,
                        Path = path,
                        UpstreamEntry = $"{sibling.Verb} {sibling.Path}",
                    });
                }
            }
        }

        return new RouteGuardPlan
        {
            Paths = paths,
            Fences = fences.OrderBy(f => f.Path, StringComparer.Ordinal).ThenBy(f => f.Verb, StringComparer.Ordinal).ToList(),
            UnguardedClusters = unguarded.ToList(),
        };
    }

    private static string Typed(Match parameter, IReadOnlyList<UpstreamOperation> declared)
    {
        var name = parameter.Groups[1].Value;
        var constraints = declared.Select(d => d.Constraints.GetValueOrDefault(name)).Distinct().ToList();
        return constraints is [{ } constraint] ? $"{{{name}:{constraint}}}" : parameter.Value;
    }

    private static string? Mount(ProxyRoute route, string upstreamPath)
    {
        // A path-mapped segment is refilled from a claim, so the caller cannot reach a sibling there.
        var mapped = route.Group.PathMap is { } map
            ? map.Covers(upstreamPath) ? map.ToBff(upstreamPath) : null
            : upstreamPath;
        return mapped is null ? null : (route.RemovePrefix ?? string.Empty) + mapped;
    }

    /// <summary>Whether the upstream would route some URL that <paramref name="route"/> matches to <paramref name="sibling"/> instead.</summary>
    private static bool Outranks(IReadOnlyList<Segment> sibling, IReadOnlyList<Segment> route)
    {
        var open = route is [.., { Kind: SegmentKind.CatchAll }];
        var fixedCount = open ? route.Count - 1 : route.Count;
        if (sibling.Any(s => s.Kind == SegmentKind.CatchAll) || (open ? sibling.Count < fixedCount : sibling.Count != route.Count))
            return false;

        for (var i = 0; i < fixedCount; i++)
        {
            if (sibling[i].Kind == SegmentKind.Literal && route[i].Kind == SegmentKind.Literal
                && !string.Equals(sibling[i].Text, route[i].Text, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        for (var i = 0; i < fixedCount; i++)
        {
            if (sibling[i].Kind != route[i].Kind) return sibling[i].Kind < route[i].Kind;
        }

        return open;
    }

    private static List<UpstreamOperation> Read(JsonObject spec)
    {
        var operations = new List<UpstreamOperation>();
        foreach (var (path, item) in spec["paths"] as JsonObject ?? [])
        {
            if (item is not JsonObject pathItem) continue;
            var shared = PathParameters(pathItem["parameters"]);

            foreach (var (verb, node) in pathItem)
            {
                if (node is not JsonObject operation || !HttpVerbs.Contains(verb)) continue;

                var constraints = new Dictionary<string, string?>(shared, StringComparer.Ordinal);
                foreach (var (name, constraint) in PathParameters(operation["parameters"]))
                    constraints[name] = constraint;
                operations.Add(new UpstreamOperation(verb.ToUpperInvariant(), path, Segments(path), constraints));
            }
        }

        return operations;
    }

    private static Dictionary<string, string?> PathParameters(JsonNode? parameters) =>
        (parameters as JsonArray ?? [])
            .OfType<JsonObject>()
            .Where(p => Text(p["in"]) == "path" && Text(p["name"]) is not null)
            .ToDictionary(p => Text(p["name"])!, p => Constraint(p["schema"]), StringComparer.Ordinal);

    private static string? Constraint(JsonNode? schema) => (Text(schema?["type"]), Text(schema?["format"])) switch
    {
        ("string", "uuid") => "guid",
        ("integer", "int32") => "int",
        ("integer", _) => "long",
        ("boolean", _) => "bool",
        _ => null,
    };

    private static string? Text(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;

    private static List<Segment> Segments(string template) =>
        template.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(text => new Segment(
                text.StartsWith("{*", StringComparison.Ordinal) ? SegmentKind.CatchAll
                : WholeParameter().IsMatch(text) ? SegmentKind.Parameter
                : text.Contains('{', StringComparison.Ordinal) ? SegmentKind.Complex
                : SegmentKind.Literal,
                text))
            .ToList();

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex RouteParameter();

    [GeneratedRegex(@"^\{\w+\}$")]
    private static partial Regex WholeParameter();

    private readonly record struct Segment(SegmentKind Kind, string Text);

    private sealed record UpstreamOperation(
        string Verb, string Path, IReadOnlyList<Segment> Segments, IReadOnlyDictionary<string, string?> Constraints);
}
