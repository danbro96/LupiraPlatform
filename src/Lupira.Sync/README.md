# Lupira.Sync

BCL-only building blocks for a `/sync/*` changes feed.

- `SyncCursor`: opaque `{sequence}.{scope}[.{after}]` cursor. `sequence` is the global event-sequence watermark; `scope` hashes the caller's readable container ids, so a grant or revoke restarts the stream; `after` is the last id of an unfinished full-sync page. A bare sequence parses with an empty scope (restarts once).
- `SyncFeedQuery`: parses `since` and clamps `limit` (default 200, max 500). `IsReset(scope)` = no cursor or another scope; `IsFullSync(scope)` = a reset or an unfinished full sync.
- `SyncPage<T>`: `{ cursor, hasMore, reset, changed, deleted }` response of a paged feed.
- `SectionGuardDto`: `{ ts, cmd }` last-writer guard of one section of a changed row.

Paged feed (event sequences from `Lupira.Sync.Marten`):

```csharp
if (!SyncFeedQuery.TryParse(since, limit, out var q)) return Invalid(SyncFeedQuery.InvalidSince);
var scope = SyncCursor.ScopeOf(readableIds);
if (q.IsFullSync(scope))
{
    var head = q.IsReset(scope) ? await session.HeadSequenceAsync(ct) : q.Since!.Value.Sequence;
    // page visible snapshots in SQL: ORDER BY id, id > q.Since?.After, take q.Limit + 1
    Cursor = hasMore ? new SyncCursor(head, scope) { After = lastId }.ToString() : new SyncCursor(head, scope).ToString();
}
else
{
    var changes = await session.ChangedStreamsAsync<TAggregate>(q.Since!.Value.Sequence, q.Limit, ct);
    // visible -> changed, other candidates -> deleted
    Cursor = new SyncCursor(changes.NextSequence, scope).ToString();
}
```

Unpaged feed:

```csharp
if (!SyncCursor.TryResume(since, scope, out var from)) return Invalid(...);
```

Per-section guard-set DTOs and the changed-item DTO stay in each API: their sections and item properties are domain-named.
