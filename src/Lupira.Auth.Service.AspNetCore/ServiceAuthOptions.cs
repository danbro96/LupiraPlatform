namespace Lupira.Auth.Service.AspNetCore;

/// <summary>
/// Binds <c>Auth:Service</c> — the production internal-hop auth (Authentik client-credentials). When
/// configured, a "ServiceJwt" bearer validates service tokens for the <c>Service</c> policy
/// alongside the Development header. Distinct from the app-facing <c>Auth:Oidc</c> audience.
/// </summary>
public sealed class ServiceAuthOptions
{
    public const string SectionName = "Auth:Service";
    public const string JwtSchemeName = "ServiceJwt";

    public string? Authority { get; set; }

    /// <summary>The audience Authentik stamps on client-credentials tokens minted for the internal hops.</summary>
    public string? Audience { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(Audience);

    /// <summary>The schemes the <c>Service</c> policy accepts: the dev header, plus the bearer when configured.</summary>
    public string[] AuthenticationSchemes =>
        IsConfigured ? [ServiceAuthHandler.SchemeName, JwtSchemeName] : [ServiceAuthHandler.SchemeName];
}
