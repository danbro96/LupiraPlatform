using Marten;
using Marten.Events;

namespace Lupira.Sync.Marten;

internal static class EventLogSql
{
    public static string Head(IReadOnlyStoreOptions options, bool settled) =>
        $"select seq_id from {options.Schema.ForEvents()}{(settled ? " where timestamp <= now() - ?" : string.Empty)} order by seq_id desc limit 1";

    // AdvancedSql reads each column of a multi-type result as a row().
    public static string ChangedStreams(IReadOnlyStoreOptions options, bool settled) =>
        $"""
        select row(e.stream_id), row(max(e.seq_id))
        from {options.Schema.ForEvents()} e
        join {options.Schema.ForStreams()} s on s.id = e.stream_id
        where e.seq_id > ?{(settled ? $" and e.seq_id <= coalesce(({Head(options, settled)}), 0)" : string.Empty)} and s.type = ?
        group by e.stream_id
        order by max(e.seq_id)
        limit ?
        """;

    public static string StreamTypeOf(IReadOnlyStoreOptions options, Type aggregateType) =>
        options.Events is EventGraph events
            ? events.AggregateAliasFor(aggregateType)
            : throw new InvalidOperationException($"Unsupported Marten event store options: {options.Events.GetType()}.");
}
