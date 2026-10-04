using Microsoft.Extensions.Configuration;

namespace Lupira.Depz;

public sealed class ConfigurationDependencyTargetSource : IDependencyTargetSource
{
    private readonly IReadOnlyList<DependencyTarget> _targets;

    public ConfigurationDependencyTargetSource(IConfiguration configuration, IEnumerable<ConfiguredTarget> targets) =>
        _targets = From(configuration, targets);

    public static IReadOnlyList<DependencyTarget> From(IConfiguration configuration, IEnumerable<ConfiguredTarget> targets) =>
        targets.Select(t =>
        {
            var section = configuration.GetSection(t.Section);
            return new DependencyTarget
            {
                Name = t.Name,
                BaseUrl = section[t.BaseUrlKey] ?? string.Empty,
                ProbePath = t.ProbePath,
                Credential = (t.Credential ?? ClientCredentialsFrom)(section),
            };
        }).ToList();

    /// <summary>The outbound-hop keys (<c>TokenUrl</c>, <c>ClientId</c>, <c>ClientSecret</c>, <c>Scope</c>,
    /// <c>DevUser</c>); null when none is set.</summary>
    public static IProbeCredential? ClientCredentialsFrom(IConfigurationSection section)
    {
        var credential = new ClientCredentialsProbeCredential
        {
            TokenUrl = section["TokenUrl"],
            ClientId = section["ClientId"],
            ClientSecret = section["ClientSecret"],
            Scope = section["Scope"],
            DevUser = section["DevUser"],
        };
        string?[] keys = [credential.TokenUrl, credential.ClientId, credential.ClientSecret, credential.Scope, credential.DevUser];
        return keys.All(string.IsNullOrWhiteSpace) ? null : credential;
    }

    public IReadOnlyList<DependencyTarget> GetTargets() => _targets;
}
