using Microsoft.Extensions.Configuration;

namespace Lupira.Depz;

/// <summary>An edge read from the same config section the real client binds — edges cannot drift.</summary>
public sealed class ConfiguredTarget
{
    public required string Name { get; set; }

    public required string Section { get; set; }

    public required string ProbePath { get; set; }

    public string BaseUrlKey { get; set; } = "BaseUrl";

    /// <summary>Null = <see cref="ConfigurationDependencyTargetSource.ClientCredentialsFrom"/>.</summary>
    public Func<IConfigurationSection, IProbeCredential?>? Credential { get; set; }
}
