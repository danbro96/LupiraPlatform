namespace Lupira.Auth.Service;

/// <summary>
/// The authenticated caller, reduced to the transport-neutral facts the service layer needs. Two shapes:
/// a <b>Member</b> — a real user (the <b>principal</b>) identified by <see cref="Email"/> (the OIDC subject) +
/// groups, built from the JWT bearer by each surface's adapter; or a <b>Service</b> — an internal platform
/// component on the LAN identified by <see cref="ServiceId"/> with <see cref="Email"/> null, which relays a
/// principal's <i>id</i> in the request body and never acts as itself against user records. Services stamp
/// <see cref="Actor"/> into the event <c>actor</c> header for attribution.
/// </summary>
public sealed record Caller
{
    /// <summary>Member identity (OIDC subject). <c>null</c> for a service caller.</summary>
    public string? Email { get; private init; }

    public IReadOnlyList<string> Groups { get; private init; } = [];

    /// <summary>Internal service identity (e.g. <c>cal-worker</c>). <c>null</c> for a member caller.</summary>
    public string? ServiceId { get; private init; }

    /// <summary>True only for a member in one of the app's admin groups; a service caller is never admin.</summary>
    public bool IsAdmin { get; private init; }

    private Caller()
    {
    }

    public static Caller Member(string email, IReadOnlyList<string> groups, IReadOnlyCollection<string> adminGroups) =>
        new() { Email = email, Groups = groups, IsAdmin = AdminGroups.Grants(adminGroups, groups) };

    public static Caller Service(string serviceId) =>
        new() { ServiceId = serviceId };

    public bool IsService => ServiceId is not null;

    /// <summary>The value stamped into the event <c>actor</c> header: a member's email, or
    /// <c>service:{id}</c> for an internal-hop write.</summary>
    public string Actor => IsService ? $"service:{ServiceId}" : Email!;
}
