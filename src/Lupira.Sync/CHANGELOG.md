# Changelog

## 0.2.0
- `SyncCursor.After` (optional): full-sync keyset position, formatted `{sequence}.{scope}.{after:N}`. Two-part and bare-sequence cursors parse as before.
- `SyncFeedQuery`: `TryParse(since, limit)` (default 200, max 500, clamped), `InvalidSince` message, `IsReset(scope)`, `IsFullSync(scope)`.
- `SyncPage<T>`: `cursor`, `hasMore`, `reset`, `changed`, `deleted`.

## 0.1.0
- `SyncCursor` (`{sequence}.{scope}` watermark + readable-container scope hash): `ScopeOf`, `TryParse`, `TryResume`, `ToString`. Unified from the Cal and Contact copies (Contact's `TryResume`).
- `SectionGuardDto` (`ts`, `cmd`) with `From(ts, cmd)`.
