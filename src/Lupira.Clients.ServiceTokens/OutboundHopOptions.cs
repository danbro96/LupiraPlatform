namespace Lupira.Clients.ServiceTokens;

/// <summary>Bindable <see cref="IOutboundHopOptions"/>. Unset <see cref="BaseUrl"/> ⇒ not configured.</summary>
public class OutboundHopOptions : IOutboundHopOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string? Audience { get; set; }

    public string? TokenUrl { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary>Scope for the client-credentials token — the scope mapping is what injects the target's audience.</summary>
    public string? Scope { get; set; }

    /// <summary>Local-only: sent as <c>X-Dev-User</c> when no credentials are configured.</summary>
    public string? DevUser { get; set; }

    /// <summary>Local-only: sent as <c>X-Dev-Service</c> when no credentials are configured.</summary>
    public string? DevServiceId { get; set; }

    /// <summary>Local-only: sent as <c>X-Dev-Scopes</c> alongside <see cref="DevUser"/>, e.g. <c>internal:read</c>.</summary>
    public string? DevScopes { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}
