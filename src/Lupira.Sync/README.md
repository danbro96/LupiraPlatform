# Lupira.Sync

BCL-only building blocks for a `/sync/*` changes feed.

- `SyncCursor`: opaque `{sequence}.{scope}` cursor. `scope` hashes the caller's readable container ids, so a grant or revoke restarts the stream. A bare sequence parses with an empty scope (restarts once).
- `SectionGuardDto`: `{ ts, cmd }` last-writer guard of one section of a changed row.

Paged feed:

```csharp
var scope = SyncCursor.ScopeOf(readableIds);
var reset = given?.Scope != scope;
var from = reset ? 0 : given!.Value.Sequence;
// ...
Cursor = new SyncCursor(next, scope).ToString();
```

Unpaged feed:

```csharp
if (!SyncCursor.TryResume(since, scope, out var from)) return Invalid(...);
```

The changes-response and per-section guard-set DTOs stay in each API: their item property (`item`, `contact`) and sections are domain-named.

