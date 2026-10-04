namespace Lupira.Clients.ServiceTokens;

/// <summary>Binds <c>Auth:Exchange</c> — the service's confidential client as the RFC 8693 requester. The target
/// providers federate this client.</summary>
public sealed class TokenExchangeOptions : IConfidentialClient
{
    public const string SectionName = "Auth:Exchange";

    public string? TokenUrl { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string Scope { get; set; } = "openid profile email";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TokenUrl) && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
