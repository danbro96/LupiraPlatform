using System.Text.Json.Nodes;

namespace Lupira.Bff.OpenApi;

public sealed class UpstreamDocument
{
    public required string Cluster { get; set; }

    public required JsonObject Document { get; set; }
}
