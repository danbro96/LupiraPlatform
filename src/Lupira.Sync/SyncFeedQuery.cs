namespace Lupira.Sync;

/// <summary>A paged feed's parsed <c>since</c> and clamped <c>limit</c>.</summary>
public readonly record struct SyncFeedQuery(SyncCursor? Since, int Limit)
{
    public const int DefaultLimit = 200;
    public const int MaxLimit = 500;
    public const string InvalidSince = "since must be a cursor previously returned by this endpoint (or omitted for a full sync).";

    /// <summary>False when <paramref name="since"/> is not a cursor; the API answers 400 with <see cref="InvalidSince"/>.</summary>
    public static bool TryParse(string? since, int? limit, out SyncFeedQuery query)
    {
        query = default;
        SyncCursor? given = null;
        if (!string.IsNullOrWhiteSpace(since))
        {
            if (!SyncCursor.TryParse(since, out var parsed)) return false;
            given = parsed;
        }

        query = new SyncFeedQuery(given, Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit));
        return true;
    }

    /// <summary>The client must drop its mirror: no cursor, or one issued for another scope.</summary>
    public bool IsReset(string scope) => Since?.Scope != scope;

    /// <summary>A reset, or a later page of a full sync still in progress.</summary>
    public bool IsFullSync(string scope) => IsReset(scope) || Since!.Value.After is not null;
}
