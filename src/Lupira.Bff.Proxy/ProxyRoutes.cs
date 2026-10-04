using System.Globalization;
using System.Text.RegularExpressions;

namespace Lupira.Bff.Proxy;

/// <summary>
/// Builds <c>ReverseProxy:Routes</c> from <c>exposed.json</c> — one exact template per path, methods
/// pinned — as a configuration source.
/// </summary>
/// <remarks>
/// Fed to <c>AddInMemoryCollection</c> so YARP's <c>LoadFromConfig</c> reads it as it reads appsettings.
/// A config source rather than <c>LoadFromMemory</c> because that takes the clusters with it, and those
/// are hand-maintained.
/// </remarks>
public static partial class ProxyRoutes
{
    public static IReadOnlyList<ProxyRoute> Plan(ExposedSurface surface)
    {
        var routes = new List<ProxyRoute>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var group in surface.Groups)
        {
            var suffix = group.Name == ExposedGroup.DefaultName ? string.Empty : $"-{group.Name}";
            var byCluster = surface.Operations
                .Where(o => o.Group == group)
                .GroupBy(o => o.Cluster, StringComparer.Ordinal)
                .OrderBy(c => c.Key, StringComparer.Ordinal);

            foreach (var operations in byCluster)
            {
                var cluster = operations.Key;
                var mount = group.Prefixed ? surface.Clusters[cluster] : null;

                var batches = group.Prefixed
                    ? operations
                        .GroupBy(o => o.BffPath, StringComparer.Ordinal)
                        .OrderBy(p => p.Key, StringComparer.Ordinal)
                        .Select(p => (Path: p.Key, p.First().MappedPath, Verbs: Verbs(p), Tail: suffix))
                    : operations
                        .OrderBy(o => o.BffPath, StringComparer.Ordinal)
                        .ThenBy(o => o.Verb, StringComparer.Ordinal)
                        .Select(o => (Path: o.BffPath, o.MappedPath, Verbs: Single(o.Verb), Tail: $"-{o.Verb}{suffix}"));

                foreach (var (path, mapped, verbs, tail) in batches)
                {
                    var raw = cluster + CatchAll().Replace(mapped, string.Empty)
                        .Replace("{", string.Empty, StringComparison.Ordinal)
                        .Replace("}", string.Empty, StringComparison.Ordinal);

                    routes.Add(new ProxyRoute
                    {
                        Key = Claim(seen, Key(raw + tail), $"{group.Name} {string.Join(',', verbs)} {cluster}{path}"),
                        Group = group,
                        Cluster = cluster,
                        Path = path,
                        Verbs = verbs,
                        RemovePrefix = mount?.Prefix,
                        AnnouncesPrefix = mount?.AnnouncesPrefix ?? false,
                    });
                }
            }
        }

        return routes;
    }

    public static IReadOnlyDictionary<string, string?> Build(ExposedSurface surface)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var route in Plan(surface))
            Write(settings, route);
        return settings;
    }

    private static void Write(Dictionary<string, string?> settings, ProxyRoute route)
    {
        var at = $"ReverseProxy:Routes:{route.Key}";
        settings[$"{at}:ClusterId"] = route.Cluster;
        settings[$"{at}:AuthorizationPolicy"] = route.Group.Policy;
        settings[$"{at}:Match:Path"] = route.Path;

        var index = 0;
        foreach (var verb in route.Verbs)
            settings[$"{at}:Match:Methods:{index++.ToString(CultureInfo.InvariantCulture)}"] = verb;

        if (route.RemovePrefix is null) return;
        settings[$"{at}:Transforms:0:PathRemovePrefix"] = route.RemovePrefix;
        if (!route.AnnouncesPrefix) return;
        settings[$"{at}:Transforms:1:X-Forwarded"] = "Set";
        settings[$"{at}:Transforms:1:Prefix"] = "Off";
        settings[$"{at}:Transforms:2:RequestHeader"] = "X-Forwarded-Prefix";
        settings[$"{at}:Transforms:2:Set"] = route.RemovePrefix;
    }

    // The key is derived from cluster + path with punctuation flattened, so two paths could collide and
    // one would silently win.
    private static string Claim(Dictionary<string, string> seen, string key, string source)
    {
        if (seen.TryGetValue(key, out var prior))
            throw new InvalidOperationException($"Route key collision: {key} ({prior} vs {source}).");
        seen[key] = source;
        return key;
    }

    private static IReadOnlyList<string> Single(string verb) => [verb];

    private static IReadOnlyList<string> Verbs(IEnumerable<ExposedOperation> operations) =>
        operations.Select(o => o.Verb).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    private static string Key(string raw) => NonAlphanumeric().Replace(raw, "-").TrimEnd('-');

    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex(@"\{\*\*\w+\}")]
    private static partial Regex CatchAll();
}
