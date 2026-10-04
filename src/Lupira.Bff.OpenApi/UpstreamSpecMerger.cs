using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lupira.Bff.Proxy;

namespace Lupira.Bff.OpenApi;

/// <summary>
/// Reconstructs the BFF's own OpenAPI document from the upstream specs it proxies: mounts each
/// upstream's paths where the BFF serves them, keeps only the documented allowlisted operations, and
/// merges the schemas they reach.
/// </summary>
/// <remarks>
/// On the JSON DOM, not <c>OpenApiDocument</c>: renaming a colliding schema means retargeting every
/// <c>$ref</c> to it, and the typed model exposes referenced nodes read-only.
/// </remarks>
public static partial class UpstreamSpecMerger
{
    private const string RefPrefix = "#/components/schemas/";

    public static MergeResult Merge(
        ExposedSurface surface, IReadOnlyList<UpstreamDocument> upstreams, UpstreamSpecMergerOptions options)
    {
        var notExposed = new List<string>();
        var missing = new List<string>();
        var renames = new List<string>();

        // A mixed merge would need down-levelling: 3.1 nullables (`type: [x, 'null']`) are invalid in 3.0.
        var versions = upstreams.Select(d => d.Document["openapi"]?.GetValue<string>()).Distinct().ToArray();
        if (versions.Length != 1)
            throw new InvalidOperationException($"Upstream specs disagree on OpenAPI version: {string.Join(", ", versions)}");

        var routed = surface.Operations
            .Where(o => !o.Group.Documented)
            .Select(o => $"{o.Cluster}  {o.UpstreamEntry}")
            .ToHashSet(StringComparer.Ordinal);

        var mergedPaths = new JsonObject();
        var mergedSchemas = new JsonObject();
        var claimedSchemas = new Dictionary<string, (string Signature, string Cluster)>(StringComparer.Ordinal);
        var claimedOperations = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (cluster, doc) in upstreams.Select(u => (u.Cluster, u.Document)))
        {
            var allowed = surface.Documented
                .Where(o => o.Cluster == cluster)
                .ToDictionary(o => o.UpstreamEntry, StringComparer.Ordinal);
            var tag = options.RetagByCluster ? cluster.Replace("-api", string.Empty, StringComparison.Ordinal) : null;

            var kept = PruneToAllowlist(doc, allowed, tag, cluster, routed, notExposed);
            missing.AddRange(allowed.Keys.Select(entry => $"{cluster}  {entry}"));

            var schemas = doc["components"]?["schemas"]?.AsObject() ?? [];
            var live = ReachableSchemas(kept.Paths, schemas);

            // A name already claimed with a DIFFERENT shape gets its cluster as a namespace; identical
            // shapes dedupe for free.
            var rename = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var name in live)
            {
                var signature = schemas[name]?.ToJsonString() ?? "null";
                if (!claimedSchemas.TryGetValue(name, out var prior))
                {
                    claimedSchemas[name] = (signature, cluster);
                    continue;
                }

                if (prior.Signature == signature) continue;

                var alias = Namespaced(options, cluster, name, $"Schema {name} differs between {prior.Cluster} and {cluster}.");
                rename[name] = alias;
                claimedSchemas[alias] = (signature, cluster);
                renames.Add($"{name} ({prior.Cluster} vs {cluster}) -> {alias}");
            }

            RewriteSchemaRefs(kept.Paths, rename);

            // Operation ids name the generated functions, and they collide too — cal and tasks both
            // declare GetItem for genuinely different operations. Same rule as schemas.
            foreach (var operation in Operations(kept.Paths))
            {
                var id = operation["operationId"]?.GetValue<string>();
                if (id is null) continue;
                if (!claimedOperations.TryGetValue(id, out var prior))
                {
                    claimedOperations[id] = cluster;
                    continue;
                }

                var alias = Namespaced(options, cluster, id, $"operationId {id} is declared by both {prior} and {cluster}.");
                operation["operationId"] = alias;
                claimedOperations[alias] = cluster;
                renames.Add($"{id}() ({prior} vs {cluster}) -> {alias}()");
            }

            var paths = options.SortPaths ? kept.Paths.OrderBy(p => p.Key, StringComparer.Ordinal).ToList() : kept.Paths.ToList();
            foreach (var (path, item) in paths)
                Mount(mergedPaths, path, (JsonObject) item!, kept.Sources, options);

            foreach (var name in live)
            {
                var final = rename.GetValueOrDefault(name, name);
                var schema = schemas[name]?.DeepClone();
                if (schema is null) continue;
                RewriteSchemaRefs(schema, rename);
                mergedSchemas[final] ??= schema;
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"exposed.json lists operations no upstream declares:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", missing)}");
        }

        var securitySchemes = new JsonObject();
        foreach (var (name, scheme) in options.SecuritySchemes)
            securitySchemes[name] = scheme.DeepClone();

        var document = new JsonObject
        {
            ["openapi"] = versions[0],
            ["info"] = new JsonObject
            {
                ["title"] = options.Title,
                ["version"] = options.Version ?? upstreams.FirstOrDefault()?.Document["info"]?["version"]?.GetValue<string>() ?? "v1",
            },
            ["paths"] = mergedPaths,
            ["components"] = new JsonObject
            {
                ["schemas"] = mergedSchemas,
                ["securitySchemes"] = securitySchemes,
            },
        };

        return new MergeResult { Document = document, NotExposed = notExposed, Renames = renames };
    }

    private static string Namespaced(UpstreamSpecMergerOptions options, string cluster, string name, string conflict)
    {
        if (!options.NamespaceCollisions)
            throw new InvalidOperationException($"{conflict} Enable NamespaceCollisions to namespace it.");

        var bare = cluster.Replace("-api", string.Empty, StringComparison.Ordinal);
        return char.ToUpperInvariant(bare[0]) + bare[1..] + name;
    }

    /// <summary>A path map moves an operation's path, so its dropped route parameters must go too.</summary>
    private static void Mount(
        JsonObject mergedPaths,
        string upstreamPath,
        JsonObject item,
        Dictionary<JsonObject, ExposedOperation> sources,
        UpstreamSpecMergerOptions options)
    {
        var shared = item.Where(kv => !IsOperation(kv.Value)).ToList();
        foreach (var target in item.Where(kv => IsOperation(kv.Value)).GroupBy(kv => sources[(JsonObject) kv.Value!].BffPath, StringComparer.Ordinal))
        {
            var dropped = RouteParameters(upstreamPath).Except(RouteParameters(target.Key), StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
            var pathItem = mergedPaths[target.Key] as JsonObject ?? new JsonObject();
            foreach (var (key, value) in shared)
            {
                if (!pathItem.ContainsKey(key)) pathItem[key] = value?.DeepClone();
            }

            foreach (var (verb, node) in target)
            {
                if (pathItem.ContainsKey(verb))
                    throw new InvalidOperationException($"Two upstream operations publish {verb.ToUpperInvariant()} {target.Key}.");

                var operation = (JsonObject) node!.DeepClone();
                operation["security"] = Security(options, sources[(JsonObject) node]);
                pathItem[verb] = operation;
            }

            DropParameters(pathItem, dropped);
            mergedPaths[target.Key] = pathItem;
        }
    }

    /// <summary>
    /// Uncarried, the requirements dangle and Microsoft.OpenApi writes them as <c>[{}]</c> — which reads
    /// as "no authentication required".
    /// </summary>
    private static JsonArray Security(UpstreamSpecMergerOptions options, ExposedOperation operation)
    {
        var requirements = new JsonArray();
        foreach (var scheme in options.SecurityFor(operation))
        {
            if (!options.SecuritySchemes.ContainsKey(scheme))
                throw new InvalidOperationException($"{operation.Verb} {operation.BffPath} requires scheme {scheme}, which the document does not define.");
            requirements.Add(new JsonObject { [scheme] = new JsonArray() });
        }

        if (requirements.Count == 0)
            throw new InvalidOperationException($"{operation.Verb} {operation.BffPath} declares no security scheme.");
        return requirements;
    }

    private static void DropParameters(JsonObject pathItem, HashSet<string> dropped)
    {
        if (dropped.Count == 0) return;

        foreach (var holder in pathItem.Select(kv => kv.Value).OfType<JsonObject>().Prepend(pathItem).ToList())
        {
            if (holder["parameters"] is not JsonArray parameters) continue;

            for (var i = parameters.Count - 1; i >= 0; i--)
            {
                if (parameters[i] is JsonObject p
                    && p["in"]?.GetValue<string>() == "path"
                    && p["name"]?.GetValue<string>() is { } name
                    && dropped.Contains(name))
                {
                    parameters.RemoveAt(i);
                }
            }

            if (parameters.Count == 0) holder.Remove("parameters");
        }
    }

    /// <summary>Drops every operation the allowlist does not name, and removes what it consumed from it.</summary>
    private static (JsonObject Paths, Dictionary<JsonObject, ExposedOperation> Sources) PruneToAllowlist(
        JsonObject doc,
        Dictionary<string, ExposedOperation> allowed,
        string? tag,
        string cluster,
        HashSet<string> routed,
        List<string> notExposed)
    {
        var kept = new JsonObject();
        var sources = new Dictionary<JsonObject, ExposedOperation>(ReferenceEqualityComparer.Instance);
        foreach (var (path, item) in doc["paths"]?.AsObject() ?? [])
        {
            if (item is not JsonObject pathItem) continue;
            var keptItem = new JsonObject();
            foreach (var (verb, node) in pathItem)
            {
                if (!IsOperation(node))
                {
                    keptItem[verb] = node?.DeepClone();   // path-level `parameters` and friends
                    continue;
                }

                var entry = $"{verb.ToUpperInvariant()} {path}";
                if (!allowed.Remove(entry, out var exposed))
                {
                    if (node!.AsObject().ContainsKey("operationId") && !routed.Contains($"{cluster}  {entry}"))
                        notExposed.Add($"{cluster}  {entry}");
                    continue;
                }

                var copy = (JsonObject) node!.DeepClone();
                if (tag is not null) copy["tags"] = new JsonArray(tag);
                keptItem[verb] = copy;
                sources[copy] = exposed;
            }

            if (keptItem.Any(kv => IsOperation(kv.Value)))
                kept[path] = keptItem;
        }

        return (kept, sources);
    }

    /// <summary>Schemas the pruned paths still reach, transitively — so a dropped path drops its DTOs.</summary>
    private static List<string> ReachableSchemas(JsonNode paths, JsonObject schemas)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var order = new List<string>();
        var queue = new Queue<JsonNode>();
        queue.Enqueue(paths);

        while (queue.TryDequeue(out var node))
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (key == "$ref" && value?.GetValue<string>() is { } reference)
                        {
                            var name = SchemaName(reference);
                            if (name is null || !seen.Add(name)) continue;
                            order.Add(name);
                            if (schemas[name] is { } target) queue.Enqueue(target);
                        }
                        else if (value is not null)
                        {
                            queue.Enqueue(value);
                        }
                    }

                    break;
                case JsonArray array:
                    foreach (var value in array.Where(v => v is not null)) queue.Enqueue(value!);
                    break;
            }
        }

        return order;
    }

    /// <summary>
    /// Repoints <c>$ref</c>s at their renamed schema, structurally and scoped to one document — a
    /// text substitution across the merged file would repoint the other clusters' refs at this alias too.
    /// </summary>
    private static void RewriteSchemaRefs(JsonNode node, Dictionary<string, string> rename)
    {
        if (rename.Count == 0) return;

        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj.ToList())
                {
                    if (key == "$ref"
                        && value?.GetValue<string>() is { } reference
                        && SchemaName(reference) is { } name
                        && rename.TryGetValue(name, out var alias))
                    {
                        obj[key] = $"{RefPrefix}{alias}";
                    }
                    else if (value is not null)
                    {
                        RewriteSchemaRefs(value, rename);
                    }
                }

                break;
            case JsonArray array:
                foreach (var value in array.Where(v => v is not null)) RewriteSchemaRefs(value!, rename);
                break;
        }
    }

    private static bool IsOperation(JsonNode? node) => node is JsonObject operation && operation.ContainsKey("responses");

    private static IEnumerable<JsonObject> Operations(JsonObject paths) =>
        paths.Select(p => p.Value).OfType<JsonObject>()
            .SelectMany(item => item.Select(v => v.Value))
            .Where(IsOperation)
            .Cast<JsonObject>();

    private static IEnumerable<string> RouteParameters(string path) =>
        RouteParameter().Matches(path).Select(m => m.Groups[1].Value);

    private static string? SchemaName(string reference) =>
        reference.StartsWith(RefPrefix, StringComparison.Ordinal) ? reference[RefPrefix.Length..] : null;

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex RouteParameter();
}
