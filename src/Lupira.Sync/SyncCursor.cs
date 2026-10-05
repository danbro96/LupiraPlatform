using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Lupira.Sync;

/// <summary>Feed cursor: global event-sequence watermark + a token of the caller's readable containers. Grants and
/// revokes append no event to the records they expose, so a scope mismatch restarts the stream. <see cref="After"/> is
/// the last id of a full-sync page still in progress.</summary>
public readonly record struct SyncCursor(long Sequence, string Scope)
{
    public Guid? After { get; init; }

    public static string ScopeOf(IEnumerable<Guid> readableContainerIds)
    {
        var joined = string.Join(',', readableContainerIds.Order().Select(id => id.ToString("N")));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(joined)))[..16];
    }

    /// <summary>A pre-scope cursor (bare sequence) parses with an empty scope, so it restarts once.</summary>
    public static bool TryParse(string value, out SyncCursor cursor)
    {
        cursor = default;
        var parts = value.Split('.');
        if (parts.Length > 3) return false;
        if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)) return false;
        Guid? after = null;
        if (parts.Length == 3)
        {
            if (!Guid.TryParseExact(parts[2], "N", out var id)) return false;
            after = id;
        }

        cursor = new SyncCursor(sequence, parts.Length > 1 ? parts[1] : string.Empty) { After = after };
        return true;
    }

    /// <summary>Where an unpaged feed resumes for <paramref name="scope"/>: 0 = a full sync (no cursor, a cursor from another
    /// scope, or one at the start). False when <paramref name="since"/> is not a cursor at all.</summary>
    public static bool TryResume(string? since, string scope, out long sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(since)) return true;
        if (!TryParse(since, out var given)) return false;
        if (given.Scope == scope) sequence = given.Sequence;
        return true;
    }

    public override string ToString() => After is { } after ? $"{Sequence}.{Scope}.{after:N}" : $"{Sequence}.{Scope}";
}
