using System.Diagnostics;
using JasperFx.Events;
using Marten;

namespace Lupira.Identity.Marten;

/// <summary>
/// Reads and stamps event provenance onto the write session before its commit, so every event and document in
/// this unit of work carries it. All of it is unbackfillable, which is why it is stamped on every request.
/// Requires the matching <c>opts.Events.MetadataConfig</c> flags (headers + causation + correlation).
///
/// Deliberately separate from <c>PrincipalDirectory</c>: resolving an identity must not mutate session state,
/// so only the caller's own resolution site stamps.
/// </summary>
public static class EventActor
{
    public const string HeaderKey = "actor";
    public const string EmailHeaderKey = "actor.email";
    public const string SourceHeaderKey = "source";

    /// <summary>The writing surface, stamped as the <c>source</c> header. An email-only login (no OIDC sub)
    /// did not arrive over REST.</summary>
    public const string SourceApi = "api";
    public const string SourceDav = "dav";

    /// <summary>The actor header value, or <c>null</c> when none was stamped.</summary>
    public static string? Of(IEvent e) =>
        e.Headers is { } h && h.TryGetValue(HeaderKey, out var v) ? v as string : null;

    /// <summary>
    /// Stamps the acting principal (Marten <c>LastModifiedBy</c>), their email (<c>actor.email</c>), the writing
    /// surface (<c>source</c>), and the ambient OTel trace/span as correlation/causation.
    /// </summary>
    public static void Stamp(IDocumentSession session, Principal principal, string source)
    {
        session.LastModifiedBy = principal.Id.ToString();
        session.SetHeader(EmailHeaderKey, principal.Email);
        session.SetHeader(SourceHeaderKey, source);
        if (Activity.Current is { } a)
        {
            session.CorrelationId = a.TraceId.ToString();
            session.CausationId = a.SpanId.ToString();
        }
    }

    /// <summary>
    /// Stamps a command's provenance. <paramref name="actor"/> is the durable identity (principal id /
    /// <c>share:{label}</c>) — stamped both as the <c>actor</c> header (which aggregates project into
    /// attribution) and as Marten's <c>LastModifiedBy</c>. <paramref name="actorEmail"/> is a human-audit
    /// convenience header (<c>null</c> for a share write); <paramref name="source"/> records the writing
    /// surface (<see cref="SourceApi"/>/<see cref="SourceDav"/>). Causation is the command id; correlation
    /// is the current trace id when a trace is active.
    /// </summary>
    public static void Stamp(IDocumentSession session, string actor, string? actorEmail, Guid commandId, string source = SourceApi)
    {
        session.SetHeader(HeaderKey, actor);
        if (actorEmail is not null) session.SetHeader(EmailHeaderKey, actorEmail);
        session.SetHeader(SourceHeaderKey, source);
        session.LastModifiedBy = actor;
        session.CausationId = commandId.ToString();
        if (Activity.Current?.TraceId is { } trace && trace != default)
            session.CorrelationId = trace.ToString();
    }
}
