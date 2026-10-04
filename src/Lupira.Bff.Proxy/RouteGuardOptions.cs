using System.Text.Json.Nodes;

namespace Lupira.Bff.Proxy;

public sealed class RouteGuardOptions
{
    /// <summary>Cluster → its upstream OpenAPI document. A cluster without one keeps unconstrained templates.</summary>
    public IDictionary<string, JsonObject> Specs { get; } = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
}
