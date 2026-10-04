namespace Lupira.Clients.ServiceTokens;

/// <summary>A confidential client at the token endpoint: the credentials every grant posts, and the scope it requests.</summary>
public interface IConfidentialClient
{
    string? TokenUrl { get; }

    string? ClientId { get; }

    string? ClientSecret { get; }

    string? Scope { get; }

    bool HasCredentials =>
        !string.IsNullOrWhiteSpace(TokenUrl) && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
