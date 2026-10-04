using System.Security.Claims;
using Marten;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lupira.Identity.Marten.AspNetCore;

/// <summary>Reads the calling principal's OIDC claims (or the dev header) and resolves them — JIT-provisioning on first
/// login — to the local principal via <see cref="PrincipalDirectory{TPrincipal}"/>. With
/// <see cref="CurrentUserOptions.StampProvenance"/>, this is also where <see cref="EventActor"/> stamps provenance.</summary>
public class CurrentUser<TPrincipal>(
    IHttpContextAccessor http,
    PrincipalDirectory<TPrincipal> directory,
    IDocumentSession session,
    IOptions<CurrentUserOptions> options)
    where TPrincipal : Principal, new()
{
    public async Task<TPrincipal> GetAsync(CancellationToken ct = default)
    {
        var principal = http.HttpContext?.User ?? throw new InvalidOperationException("No HTTP context available.");

        var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = principal.FindFirstValue("name") ?? principal.Identity?.Name;
        if (sub is null && string.IsNullOrEmpty(email))
            throw new InvalidOperationException("Authenticated principal has no subject or email claim.");

        var resolved = await directory.ResolveOrProvisionAsync(sub, email, name, ct);
        // Inferring the surface from a missing sub is only valid for the caller's own identity.
        if (options.Value.StampProvenance)
            EventActor.Stamp(session, resolved, sub is null ? EventActor.SourceDav : EventActor.SourceApi);
        return resolved;
    }
}
