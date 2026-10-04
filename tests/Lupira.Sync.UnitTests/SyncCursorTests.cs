using Xunit;

namespace Lupira.Sync.UnitTests;

public class SyncCursorTests
{
    [Fact]
    public void Round_trips_through_its_string_form()
    {
        var cursor = new SyncCursor(4242, SyncCursor.ScopeOf([Guid.NewGuid()]));
        Assert.True(SyncCursor.TryParse(cursor.ToString(), out var parsed));
        Assert.Equal(cursor, parsed);
    }

    [Fact]
    public void Scope_ignores_order_and_changes_with_membership()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        Assert.Equal(SyncCursor.ScopeOf([a, b]), SyncCursor.ScopeOf([b, a]));
        Assert.NotEqual(SyncCursor.ScopeOf([a]), SyncCursor.ScopeOf([a, b]));
        Assert.NotEqual(SyncCursor.ScopeOf([]), SyncCursor.ScopeOf([a]));
    }

    [Fact]
    public void Scope_is_stable_across_releases()
    {
        var id = Guid.Parse("0190f3a1-7c2e-7d4b-9a1f-2b3c4d5e6f70");
        Assert.Equal("3dd55d855f602a17", SyncCursor.ScopeOf([id]));
    }

    [Fact]
    public void Bare_sequence_parses_with_an_empty_scope()
    {
        Assert.True(SyncCursor.TryParse("17", out var legacy));
        Assert.Equal(new SyncCursor(17, string.Empty), legacy);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData(".scope")]
    [InlineData("1e3.scope")]
    public void Rejects_what_it_never_issued(string value) => Assert.False(SyncCursor.TryParse(value, out _));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Resumes_from_zero_without_a_cursor(string? since)
    {
        Assert.True(SyncCursor.TryResume(since, "abc", out var sequence));
        Assert.Equal(0, sequence);
    }

    [Fact]
    public void Resumes_at_the_sequence_of_a_same_scope_cursor()
    {
        Assert.True(SyncCursor.TryResume("42.abc", "abc", out var sequence));
        Assert.Equal(42, sequence);
    }

    [Theory]
    [InlineData("42.other")]
    [InlineData("42")]
    public void Restarts_a_cursor_from_another_scope(string since)
    {
        Assert.True(SyncCursor.TryResume(since, "abc", out var sequence));
        Assert.Equal(0, sequence);
    }

    [Fact]
    public void Resume_rejects_a_non_cursor() => Assert.False(SyncCursor.TryResume("nope", "abc", out _));
}
