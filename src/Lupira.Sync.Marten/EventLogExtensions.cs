using Marten;

namespace Lupira.Sync.Marten;

/// <summary>Change detection from the event history: a record changed since sequence N if its stream has an event
/// after N. Needs Guid stream identity and streams started with <c>StartStream&lt;TAggregate&gt;</c> (which records
/// the stream type). Only events older than a settle lag (by the database clock) count, so a transaction that took a
/// lower sequence but commits after a higher one is not skipped; <see cref="TimeSpan.Zero"/> = no fence.</summary>
public static class EventLogExtensions
{
    public static readonly TimeSpan DefaultSettleLag = TimeSpan.FromSeconds(5);

    public static Task<long> HeadSequenceAsync(this IQuerySession session, CancellationToken ct = default) =>
        session.HeadSequenceAsync(DefaultSettleLag, ct);

    /// <summary>The latest global event sequence older than <paramref name="settleLag"/> (0 when none).</summary>
    public static async Task<long> HeadSequenceAsync(this IQuerySession session, TimeSpan settleLag, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(settleLag, TimeSpan.Zero);
        var settled = settleLag > TimeSpan.Zero;
        object[] parameters = settled ? [settleLag] : [];
        var head = await session.AdvancedSql.QueryAsync<long>(EventLogSql.Head(session.DocumentStore.Options, settled), ct, parameters);
        return head.Count > 0 ? head[0] : 0L;
    }

    public static Task<ChangedStreams> ChangedStreamsAsync<TAggregate>(this IQuerySession session, long since, int limit, CancellationToken ct = default) =>
        session.ChangedStreamsAsync<TAggregate>(since, limit, DefaultSettleLag, ct);

    public static Task<ChangedStreams> ChangedStreamsAsync<TAggregate>(this IQuerySession session, long since, int limit, TimeSpan settleLag, CancellationToken ct = default) =>
        session.ChangedStreamsAsync(since, EventLogSql.StreamTypeOf(session.DocumentStore.Options, typeof(TAggregate)), limit, settleLag, ct);

    public static Task<ChangedStreams> ChangedStreamsAsync(this IQuerySession session, long since, string streamType, int limit, CancellationToken ct = default) =>
        session.ChangedStreamsAsync(since, streamType, limit, DefaultSettleLag, ct);

    /// <summary>Up to <paramref name="limit"/> streams of <paramref name="streamType"/> (Marten's aggregate alias) with an
    /// event after <paramref name="since"/>, each once, ordered by their latest event; events up to the
    /// <see cref="HeadSequenceAsync(IQuerySession, TimeSpan, CancellationToken)"/> head only.</summary>
    public static async Task<ChangedStreams> ChangedStreamsAsync(this IQuerySession session, long since, string streamType, int limit, TimeSpan settleLag, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentOutOfRangeException.ThrowIfLessThan(settleLag, TimeSpan.Zero);
        var settled = settleLag > TimeSpan.Zero;
        object[] parameters = settled ? [since, settleLag, streamType, limit + 1] : [since, streamType, limit + 1];
        var rows = await session.AdvancedSql.QueryAsync<Guid, long>(EventLogSql.ChangedStreams(session.DocumentStore.Options, settled), ct, parameters);
        var hasMore = rows.Count > limit;
        var page = hasMore ? rows.Take(limit).ToList() : [.. rows];
        return new ChangedStreams
        {
            Ids = [.. page.Select(r => r.Item1)],
            NextSequence = page.Count > 0 ? page[^1].Item2 : since,
            HasMore = hasMore,
        };
    }
}
