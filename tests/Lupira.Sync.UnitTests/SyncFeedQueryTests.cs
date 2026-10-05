using Xunit;

namespace Lupira.Sync.UnitTests;

public class SyncFeedQueryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void No_cursor_is_a_reset(string? since)
    {
        Assert.True(SyncFeedQuery.TryParse(since, null, out var q));
        Assert.Null(q.Since);
        Assert.True(q.IsReset("abc"));
        Assert.True(q.IsFullSync("abc"));
    }

    [Theory]
    [InlineData(null, SyncFeedQuery.DefaultLimit)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(50, 50)]
    [InlineData(10_000, SyncFeedQuery.MaxLimit)]
    public void Clamps_the_limit(int? limit, int expected)
    {
        Assert.True(SyncFeedQuery.TryParse(null, limit, out var q));
        Assert.Equal(expected, q.Limit);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("1.abc.not-a-guid")]
    public void Rejects_a_non_cursor(string since) => Assert.False(SyncFeedQuery.TryParse(since, null, out _));

    [Fact]
    public void Same_scope_cursor_is_a_delta()
    {
        Assert.True(SyncFeedQuery.TryParse("42.abc", 10, out var q));
        Assert.Equal(new SyncCursor(42, "abc"), q.Since);
        Assert.False(q.IsReset("abc"));
        Assert.False(q.IsFullSync("abc"));
    }

    [Theory]
    [InlineData("42.other")]
    [InlineData("42")]
    [InlineData("42.other.0190f3a17c2e7d4b9a1f2b3c4d5e6f70")]
    public void Cursor_from_another_scope_is_a_reset(string since)
    {
        Assert.True(SyncFeedQuery.TryParse(since, null, out var q));
        Assert.True(q.IsReset("abc"));
        Assert.True(q.IsFullSync("abc"));
    }

    [Fact]
    public void Same_scope_cursor_with_a_position_continues_the_full_sync()
    {
        Assert.True(SyncFeedQuery.TryParse("42.abc.0190f3a17c2e7d4b9a1f2b3c4d5e6f70", null, out var q));
        Assert.False(q.IsReset("abc"));
        Assert.True(q.IsFullSync("abc"));
        Assert.Equal(Guid.Parse("0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70"), q.Since!.Value.After);
    }
}
