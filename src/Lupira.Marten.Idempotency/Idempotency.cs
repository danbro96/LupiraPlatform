using JasperFx;
using Marten;

namespace Lupira.Marten.Idempotency;

/// <summary>
/// Offline-first idempotency gate. A mutation may carry an <c>Idempotency-Key</c> header (a client-minted GUIDv7
/// command id); a mobile outbox resends the same key after a lost response, so a redelivered command must be a no-op
/// returning the prior result.
/// <para>The dedup row and the event append share ONE <see cref="IDocumentSession"/> and ONE
/// <c>SaveChangesAsync</c>. The row goes in via <c>Insert</c> — a plain INSERT, not an upsert — so a concurrent
/// duplicate violates the <see cref="ProcessedCommand"/> primary key and rolls back the whole transaction including
/// the loser's staged events, which the loser treats as idempotent success. Closes the check-then-write TOCTOU an
/// upsert would leave open.</para>
/// <para>Without a key the append simply commits — no cross-request dedup.</para>
/// </summary>
public sealed class Idempotency(IDocumentSession session)
{
    /// <summary>True when a <c>SaveChangesAsync</c> failure is the dedup race being lost — the caller should
    /// re-read and return the existing aggregate.</summary>
    public static bool IsDuplicate(Exception ex) => ex is DocumentAlreadyExistsException;

    /// <summary>The <see cref="ProcessedCommand"/> already recorded for <paramref name="commandId"/>, or null when
    /// the command is new (or no key was supplied). Callers return the existing aggregate on a hit.</summary>
    public async Task<ProcessedCommand?> SeenAsync(Guid? commandId, CancellationToken ct) =>
        commandId is { } key ? await session.LoadAsync<ProcessedCommand>(key, ct) : null;

    /// <summary>Commit the staged events together with the ledger row for <paramref name="commandId"/>, in one
    /// transaction. False when the dedup race was lost — another request with the same key committed first — and the
    /// caller returns the already-committed state (idempotent success).</summary>
    public async Task<bool> CommitAsync(Guid? commandId, Guid aggregateId, int resultVersion, CancellationToken ct)
    {
        Record(commandId, aggregateId, resultVersion);
        try
        {
            await session.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (IsDuplicate(ex))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Appends events to an existing stream and records the dedup ledger row in the same session, committed by a
    /// single <c>SaveChangesAsync</c>. Returns the resulting version, or <c>null</c> when the commit lost the race.
    /// <para>The version comes from the stream's CURRENT head (<c>FetchStreamStateAsync</c>, not the loaded
    /// snapshot's possibly-stale <c>Version</c>) plus the event count, stored in
    /// <see cref="ProcessedCommand.ResultVersion"/>. A pre-save <c>StreamAction.Version</c> would not do: the
    /// Quick append modes assign it server-side at INSERT, so it reads 0 beforehand.</para>
    /// </summary>
    public async Task<int?> AppendDedupAsync(Guid? commandId, Guid aggregateId, IReadOnlyList<object> events, CancellationToken ct)
    {
        var state = await session.Events.FetchStreamStateAsync(aggregateId, ct);
        var version = (int) (state?.Version ?? 0) + events.Count;

        session.Events.Append(aggregateId, events.ToArray());
        return await CommitAsync(commandId, aggregateId, version, ct) ? version : null;
    }

    /// <summary>Stage the ledger row alongside already-staged events; the caller owns the single
    /// <c>SaveChangesAsync</c> so it can catch the duplicate-key rollback via <see cref="IsDuplicate"/>.</summary>
    public void Record(Guid? commandId, Guid aggregateId, int resultVersion)
    {
        if (commandId is { } id)
        {
            session.Insert(new ProcessedCommand
            {
                CommandId = id,
                AggregateId = aggregateId,
                ResultVersion = resultVersion,
                ProcessedAt = DateTimeOffset.UtcNow,
            });
        }
    }
}
