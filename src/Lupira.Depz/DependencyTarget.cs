namespace Lupira.Depz;

/// <summary>One outward edge: where, and the credential to probe it as (null = anonymous).</summary>
public sealed class DependencyTarget
{
    public required string Name { get; set; }

    public required string BaseUrl { get; set; }

    public required string ProbePath { get; set; }

    public IProbeCredential? Credential { get; set; }
}
