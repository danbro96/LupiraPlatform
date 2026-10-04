namespace Lupira.Hosting.OpenApi;

internal sealed class MarkerHeader(Type markerType, string name, string description)
{
    public Type MarkerType { get; } = markerType;

    public string Name { get; } = name;

    public string Description { get; } = description;
}
