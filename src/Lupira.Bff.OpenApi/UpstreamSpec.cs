using System.Reflection;
using System.Text.Json.Nodes;

namespace Lupira.Bff.OpenApi;

public sealed class UpstreamSpec
{
    public required string Cluster { get; set; }

    public required string Name { get; set; }

    public UpstreamDocument Load(Assembly assembly)
    {
        var suffix = $"upstream.{Name}.json";
        var resource = assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n == suffix || n.EndsWith("." + suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource {suffix} is missing from {assembly.GetName().Name}.");

        using var stream = assembly.GetManifestResourceStream(resource)!;
        return new UpstreamDocument
        {
            Cluster = Cluster,
            Document = JsonNode.Parse(stream)?.AsObject()
                ?? throw new InvalidOperationException($"{resource} is not a JSON object."),
        };
    }
}
