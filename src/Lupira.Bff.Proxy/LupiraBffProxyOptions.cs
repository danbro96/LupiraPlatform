namespace Lupira.Bff.Proxy;

public sealed class LupiraBffProxyOptions
{
    /// <summary>Null loads the application assembly's embedded <c>exposed.json</c>.</summary>
    public ExposedSurface? Surface { get; set; }
}
