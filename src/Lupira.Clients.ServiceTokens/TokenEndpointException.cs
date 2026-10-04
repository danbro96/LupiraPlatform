namespace Lupira.Clients.ServiceTokens;

public sealed class TokenEndpointException(TokenErrorKind kind, int? statusCode, string? description)
    : Exception($"Token endpoint refused ({kind}, {statusCode?.ToString() ?? "no response"}): {description}")
{
    public TokenErrorKind Kind { get; } = kind;

    public int? StatusCode { get; } = statusCode;

    public string? Description { get; } = description;
}
