namespace Lupira.Clients.ServiceTokens;

/// <summary>What one outbound hop needs to authenticate: the target <see cref="Audience"/> for member token exchange,
/// the client-credentials fallback for service calls, and the dev headers sent when no credentials are configured.</summary>
public interface IOutboundHopOptions : IConfidentialClient
{
    string? Audience => null;

    string? DevUser => null;

    string? DevServiceId => null;

    string? DevScopes => null;

    bool IsConfigured { get; }
}
