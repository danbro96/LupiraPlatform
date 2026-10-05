# Changelog

## 0.1.0

- `engine`: `createSyncEngine({ openDb, modules, cacheVersion, hooks?, onChange, now? })` → `sync`, `push`, `ready`, `enqueue(ops, { holdMs? })`, `discard`, `retry`, `reindex`, `wipe`, `doc`, `docs`, `parked`, `status`.
- `types`: `AggregateModule`, `Feed`, `FeedPage`, `IndexSpec`, `OpBase`, `DocState`, `ChangeEvent`, `ParkedOp`.
- Kernel tables `docs` (a stable read surface: apps join it from their index tables), `outbox`, `cursors`, `meta`; every write folds the server doc through the queued ops (`local`), rewrites the module's index rows and reports the change after commit.
- Pull: paged feeds per module in order, resumable full syncs by generation, prune of unmentioned docs without queued ops.
- Push: single-flight drain with one queued rerun, head-of-line hold per hold key, backoff, park, and a 401 pause until the next sync; a timer replays held and backed-off ops once due.
- `status`: framework-free store for `useSyncExternalStore`; `bannerState`: the sync banner derived from it.
- `expo/triggers`: `startSyncTriggers(engine, { backgroundTaskName })` and `defineSyncTask(name, engine, prepare?)`; `prepare` runs before the background sync.
