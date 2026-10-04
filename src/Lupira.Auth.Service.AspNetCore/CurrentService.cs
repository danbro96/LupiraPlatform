using Microsoft.AspNetCore.Http;

namespace Lupira.Auth.Service.AspNetCore;

/// <summary>
/// Scoped accessor for an authenticated internal-service caller (the <see cref="ServiceAuthHandler"/>
/// scheme). Reads the <c>service_id</c> claim; handlers turn it into <c>Caller.Service(id)</c>.
/// </summary>
public sealed class CurrentService(IHttpContextAccessor accessor)
{
    /// <summary>The service identity — the dev-header claim, or the client/subject claim of an
    /// Authentik client-credentials token (azp/client_id/sub).</summary>
    public string? ServiceId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            if (user is null) return null;
            return user.FindFirst(ServiceAuthHandler.ServiceIdClaim)?.Value
                ?? user.FindFirst("azp")?.Value
                ?? user.FindFirst("client_id")?.Value
                ?? user.FindFirst("sub")?.Value;
        }
    }

    public Caller RequireCaller() =>
        Caller.Service(ServiceId ?? throw new InvalidOperationException("No authenticated service identity."));
}
