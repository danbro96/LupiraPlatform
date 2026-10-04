namespace Lupira.Identity.Marten.AspNetCore;

public sealed class CurrentUserOptions
{
    /// <summary>Stamp <see cref="EventActor"/> provenance on the request's session after resolving the caller.</summary>
    public bool StampProvenance { get; set; }
}
