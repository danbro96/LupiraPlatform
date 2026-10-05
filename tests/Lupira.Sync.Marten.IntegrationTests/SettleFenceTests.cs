using Marten.Services;
using Npgsql;
using Xunit;

namespace Lupira.Sync.Marten.IntegrationTests;

[Collection("integration")]
public sealed class SettleFenceTests(EventStoreFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan Lag = TimeSpan.FromSeconds(1);

    public Task InitializeAsync() => fixture.Store.Advanced.ResetAllData();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Events_younger_than_the_lag_are_neither_head_nor_changed()
    {
        var note = Guid.NewGuid();
        await fixture.WriteAsync<Note>(note, new Edited("started"));
        await fixture.WriteAsync<Note>(note, new Edited("appended"));

        await using var session = fixture.Store.QuerySession();
        Assert.Equal(0, await session.HeadSequenceAsync());
        Assert.Empty((await session.ChangedStreamsAsync<Note>(0, 10)).Ids);
        Assert.Equal(0, await session.HeadSequenceAsync(Lag));
        var changed = await session.ChangedStreamsAsync<Note>(0, 10, Lag);
        Assert.Empty(changed.Ids);
        Assert.Equal(0, changed.NextSequence);
    }

    [Fact]
    public async Task Events_count_once_older_than_the_lag()
    {
        var note = Guid.NewGuid();
        await fixture.WriteAsync<Note>(note, new Edited("started"));
        await fixture.WriteAsync<Note>(note, new Edited("appended"));
        await Task.Delay(Lag + TimeSpan.FromMilliseconds(200));

        await using var session = fixture.Store.QuerySession();
        var head = await session.HeadSequenceAsync(Lag);
        var changed = await session.ChangedStreamsAsync<Note>(0, 10, Lag);

        Assert.Equal(await session.HeadSequenceAsync(TimeSpan.Zero), head);
        Assert.Equal([note], changed.Ids);
        Assert.Equal(head, changed.NextSequence);
    }

    [Fact]
    public async Task A_lower_sequence_committed_after_a_higher_one_reaches_the_next_delta()
    {
        var early = Guid.NewGuid();
        var late = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var open = await connection.BeginTransactionAsync();
        await using (var pending = fixture.Store.LightweightSession(SessionOptions.ForTransaction(open)))
        {
            pending.Events.StartStream<Note>(early, new Edited("early"));
            await pending.SaveChangesAsync();
        }

        await fixture.WriteAsync<Note>(late, new Edited("late"));

        await using var session = fixture.Store.QuerySession();
        Assert.Equal([late], (await session.ChangedStreamsAsync<Note>(0, 10, TimeSpan.Zero)).Ids);
        var first = await session.ChangedStreamsAsync<Note>(0, 10, Lag);
        Assert.Empty(first.Ids);

        await open.CommitAsync();
        await Task.Delay(Lag + TimeSpan.FromMilliseconds(200));

        var next = await session.ChangedStreamsAsync<Note>(first.NextSequence, 10, Lag);
        Assert.Equal([early, late], next.Ids);
    }

    [Fact]
    public async Task A_negative_lag_is_rejected()
    {
        await using var session = fixture.Store.QuerySession();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.HeadSequenceAsync(TimeSpan.FromSeconds(-1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.ChangedStreamsAsync<Note>(0, 10, TimeSpan.FromSeconds(-1)));
    }
}
