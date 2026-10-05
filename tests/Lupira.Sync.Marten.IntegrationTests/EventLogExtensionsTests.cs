using Marten;
using Xunit;

namespace Lupira.Sync.Marten.IntegrationTests;

[Collection("integration")]
public sealed class EventLogExtensionsTests(EventStoreFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan Unfenced = TimeSpan.Zero;

    public Task InitializeAsync() => fixture.Store.Advanced.ResetAllData();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Head_is_zero_on_an_empty_store()
    {
        await using var session = fixture.Store.QuerySession();
        Assert.Equal(0, await session.HeadSequenceAsync(Unfenced));
    }

    [Fact]
    public async Task Head_is_the_latest_event_sequence()
    {
        await fixture.WriteAsync<Note>(Guid.NewGuid(), new Edited("a"), new Edited("b"));
        await fixture.WriteAsync<Folder>(Guid.NewGuid(), new Edited("c"));

        await using var session = fixture.Store.QuerySession();
        var latest = (await session.Events.QueryAllRawEvents().ToListAsync()).Max(e => e.Sequence);
        Assert.Equal(latest, await session.HeadSequenceAsync(Unfenced));
    }

    [Fact]
    public async Task Only_streams_of_the_aggregate_type_are_returned()
    {
        var note = Guid.NewGuid();
        var folder = Guid.NewGuid();
        await fixture.WriteAsync<Note>(note, new Edited("a"));
        await fixture.WriteAsync<Folder>(folder, new Edited("b"));

        await using var session = fixture.Store.QuerySession();
        var typed = await session.ChangedStreamsAsync<Note>(0, 10, Unfenced);
        var named = await session.ChangedStreamsAsync(0, "note", 10, Unfenced);

        Assert.Equal([note], typed.Ids);
        Assert.Equal(typed.Ids, named.Ids);
        Assert.Equal(typed.NextSequence, named.NextSequence);
        Assert.Equal([folder], (await session.ChangedStreamsAsync<Folder>(0, 10, Unfenced)).Ids);
    }

    [Fact]
    public async Task Only_streams_with_an_event_after_since_are_returned()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await fixture.WriteAsync<Note>(a, new Edited("a1"));
        await fixture.WriteAsync<Note>(b, new Edited("b1"));
        long since;
        await using (var session = fixture.Store.QuerySession()) since = await session.HeadSequenceAsync(Unfenced);
        await fixture.WriteAsync<Note>(a, new Edited("a2"));

        await using var query = fixture.Store.QuerySession();
        var changed = await query.ChangedStreamsAsync<Note>(since, 10, Unfenced);

        Assert.Equal([a], changed.Ids);
        Assert.Equal(await query.HeadSequenceAsync(Unfenced), changed.NextSequence);
        Assert.False(changed.HasMore);
    }

    [Fact]
    public async Task Nothing_changed_resumes_where_it_was()
    {
        await fixture.WriteAsync<Note>(Guid.NewGuid(), new Edited("a"));

        await using var session = fixture.Store.QuerySession();
        var head = await session.HeadSequenceAsync(Unfenced);
        var changed = await session.ChangedStreamsAsync<Note>(head, 10, Unfenced);

        Assert.Empty(changed.Ids);
        Assert.Equal(head, changed.NextSequence);
        Assert.False(changed.HasMore);
    }

    [Fact]
    public async Task Pages_cover_every_changed_stream_once_in_order_of_its_latest_event()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        await fixture.WriteAsync<Note>(a, new Edited("a1"), new Edited("a2"), new Edited("a3"));
        await fixture.WriteAsync<Note>(b, new Edited("b1"));
        await fixture.WriteAsync<Folder>(Guid.NewGuid(), new Edited("other"));
        await fixture.WriteAsync<Note>(c, new Edited("c1"));
        await fixture.WriteAsync<Note>(a, new Edited("a4"));
        await fixture.WriteAsync<Note>(d, new Edited("d1"));
        await fixture.WriteAsync<Note>(b, new Edited("b2"), new Edited("b3"));

        await using var session = fixture.Store.QuerySession();
        var lastSequence = (await session.Events.QueryAllRawEvents().ToListAsync())
            .GroupBy(e => e.StreamId)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Sequence));

        var first = await session.ChangedStreamsAsync<Note>(0, 2, Unfenced);
        Assert.Equal([c, a], first.Ids);
        Assert.Equal(lastSequence[a], first.NextSequence);
        Assert.True(first.HasMore);

        var second = await session.ChangedStreamsAsync<Note>(first.NextSequence, 2, Unfenced);
        Assert.Equal([d, b], second.Ids);
        Assert.Equal(lastSequence[b], second.NextSequence);
        Assert.False(second.HasMore);

        Assert.Equal(await session.HeadSequenceAsync(Unfenced), second.NextSequence);
    }

    [Fact]
    public async Task A_stream_changed_again_after_its_page_returns_on_the_next_page()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        await fixture.WriteAsync<Note>(a, new Edited("a1"));
        await fixture.WriteAsync<Note>(b, new Edited("b1"));
        await fixture.WriteAsync<Note>(c, new Edited("c1"));

        await using var session = fixture.Store.QuerySession();
        var first = await session.ChangedStreamsAsync<Note>(0, 1, Unfenced);
        Assert.Equal([a], first.Ids);

        await fixture.WriteAsync<Note>(a, new Edited("a2"));
        var second = await session.ChangedStreamsAsync<Note>(first.NextSequence, 10, Unfenced);

        Assert.Equal([b, c, a], second.Ids);
        Assert.False(second.HasMore);
    }

    [Fact]
    public async Task Exactly_limit_streams_has_no_more()
    {
        await fixture.WriteAsync<Note>(Guid.NewGuid(), new Edited("a"));
        await fixture.WriteAsync<Note>(Guid.NewGuid(), new Edited("b"));

        await using var session = fixture.Store.QuerySession();
        var changed = await session.ChangedStreamsAsync<Note>(0, 2, Unfenced);

        Assert.Equal(2, changed.Ids.Count);
        Assert.False(changed.HasMore);
    }
}
