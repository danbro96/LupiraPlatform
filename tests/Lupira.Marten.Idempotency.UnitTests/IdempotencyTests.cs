using System.Runtime.CompilerServices;
using JasperFx;
using Xunit;

namespace Lupira.Marten.Idempotency.UnitTests;

public class IdempotencyTests
{
    private static readonly Guid Aggregate = Guid.Parse("0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70");

    [Fact]
    public async Task Seen_is_null_without_a_key()
    {
        var (session, fake) = SessionFake.Create();
        Assert.Null(await new Idempotency(session).SeenAsync(null, CancellationToken.None));
        Assert.Empty(fake.Calls);
    }

    [Fact]
    public async Task Seen_returns_the_recorded_row()
    {
        var (session, fake) = SessionFake.Create();
        var key = Guid.NewGuid();
        var row = new ProcessedCommand { CommandId = key, AggregateId = Aggregate, ResultVersion = 3 };
        fake.Ledger[key] = row;
        Assert.Same(row, await new Idempotency(session).SeenAsync(key, CancellationToken.None));
        Assert.Null(await new Idempotency(session).SeenAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public void Record_stages_a_row_and_does_not_save()
    {
        var (session, fake) = SessionFake.Create();
        var key = Guid.NewGuid();
        new Idempotency(session).Record(key, Aggregate, 4);
        var row = Assert.IsType<ProcessedCommand>(Assert.Single(fake.Inserted));
        Assert.Equal((key, Aggregate, 4), (row.CommandId, row.AggregateId, row.ResultVersion));
        Assert.DoesNotContain("SaveChangesAsync", fake.Calls);
    }

    [Fact]
    public void Record_without_a_key_stages_nothing()
    {
        var (session, fake) = SessionFake.Create();
        new Idempotency(session).Record(null, Aggregate, 4);
        Assert.Empty(fake.Inserted);
    }

    [Fact]
    public void Only_a_duplicate_document_is_a_lost_race()
    {
        Assert.True(Idempotency.IsDuplicate(Duplicate()));
        Assert.False(Idempotency.IsDuplicate(new InvalidOperationException()));
    }

    [Fact]
    public async Task Commit_stages_the_row_then_saves()
    {
        var (session, fake) = SessionFake.Create();
        Assert.True(await new Idempotency(session).CommitAsync(Guid.NewGuid(), Aggregate, 2, CancellationToken.None));
        Assert.Equal(["Insert", "SaveChangesAsync"], fake.Calls);
    }

    [Fact]
    public async Task Commit_reports_a_lost_race_as_false()
    {
        var (session, fake) = SessionFake.Create();
        fake.SaveFailure = Duplicate();
        Assert.False(await new Idempotency(session).CommitAsync(Guid.NewGuid(), Aggregate, 2, CancellationToken.None));
    }

    [Fact]
    public async Task Commit_rethrows_other_failures()
    {
        var (session, fake) = SessionFake.Create();
        fake.SaveFailure = new InvalidOperationException();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new Idempotency(session).CommitAsync(Guid.NewGuid(), Aggregate, 2, CancellationToken.None));
    }

    [Fact]
    public async Task Append_versions_from_the_current_head_plus_the_event_count()
    {
        var (session, fake) = SessionFake.Create();
        fake.StreamVersion = 5;
        object[] events = ["a", "b"];
        Assert.Equal(7, await new Idempotency(session).AppendDedupAsync(Guid.NewGuid(), Aggregate, events, CancellationToken.None));
        Assert.Equal(Aggregate, Assert.Single(fake.Appended).Stream);
        Assert.Equal(7, Assert.IsType<ProcessedCommand>(Assert.Single(fake.Inserted)).ResultVersion);
        Assert.Equal(["get_Events", "FetchStreamStateAsync", "get_Events", "Append", "Insert", "SaveChangesAsync"], fake.Calls);
    }

    [Fact]
    public async Task Append_to_a_missing_stream_starts_at_the_event_count()
    {
        var (session, _) = SessionFake.Create();
        Assert.Equal(1, await new Idempotency(session).AppendDedupAsync(null, Aggregate, ["a"], CancellationToken.None));
    }

    [Fact]
    public async Task Append_reports_a_lost_race_as_null()
    {
        var (session, fake) = SessionFake.Create();
        fake.SaveFailure = Duplicate();
        Assert.Null(await new Idempotency(session).AppendDedupAsync(Guid.NewGuid(), Aggregate, ["a"], CancellationToken.None));
    }

    private static Exception Duplicate() => (Exception) RuntimeHelpers.GetUninitializedObject(typeof(DocumentAlreadyExistsException));
}
