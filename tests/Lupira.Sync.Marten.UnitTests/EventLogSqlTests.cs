using Marten;
using Xunit;

namespace Lupira.Sync.Marten.UnitTests;

public class EventLogSqlTests
{
    [Fact]
    public void Stream_type_is_the_aggregate_alias()
    {
        Assert.Equal("calendar_item", EventLogSql.StreamTypeOf(Options(), typeof(CalendarItem)));
    }

    [Fact]
    public void Queries_target_the_event_schema()
    {
        var options = Options(o => o.Events.DatabaseSchemaName = "evs");
        Assert.Contains("from evs.mt_events", EventLogSql.Head(options, settled: true), StringComparison.Ordinal);
        var changed = EventLogSql.ChangedStreams(options, settled: true);
        Assert.Contains("from evs.mt_events e", changed, StringComparison.Ordinal);
        Assert.Contains("join evs.mt_streams s on s.id = e.stream_id", changed, StringComparison.Ordinal);
    }

    [Fact]
    public void Event_schema_defaults_to_the_document_schema()
    {
        var options = Options(o => o.DatabaseSchemaName = "docs");
        Assert.Contains("from docs.mt_events", EventLogSql.Head(options, settled: false), StringComparison.Ordinal);
    }

    [Fact]
    public void Settled_head_counts_events_older_than_the_lag_by_the_database_clock()
    {
        Assert.Equal("select seq_id from public.mt_events where timestamp <= now() - ? order by seq_id desc limit 1", EventLogSql.Head(Options(), settled: true));
        Assert.Equal("select seq_id from public.mt_events order by seq_id desc limit 1", EventLogSql.Head(Options(), settled: false));
    }

    [Fact]
    public void Settled_changed_streams_stop_at_the_settled_head()
    {
        var options = Options();
        Assert.Contains($"e.seq_id <= coalesce(({EventLogSql.Head(options, settled: true)}), 0)", EventLogSql.ChangedStreams(options, settled: true), StringComparison.Ordinal);
        Assert.DoesNotContain("now()", EventLogSql.ChangedStreams(options, settled: false), StringComparison.Ordinal);
    }

    private static IReadOnlyStoreOptions Options(Action<StoreOptions>? configure = null)
    {
        var options = new StoreOptions();
        options.Connection("Host=localhost;Database=unused");
        configure?.Invoke(options);
        return options;
    }
}
