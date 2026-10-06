# Changelog

## 0.1.2

- Index rebuilds (on a `version` change at open, and `reindex`) drop and create the module's tables through `applySchema` on the main connection, then rewrite the docs in an exclusive transaction; reads no longer fail with `no such table` after a rebuild.
- A rebuild cut short between the schema change and the rewrite is redone on the next open.
- Requires `@danbro96/lupira-expo-sqlite` ^0.4.1.

## 0.1.1

- `expo/triggers`: `startSyncTriggers` takes `registerBackgroundTask` (default true); false removes the background task instead of registering it, for development builds where a fired job would start React before the dev launcher.

## 0.1.0

- `engine`: `createSyncEngine({ openDb, modules, cacheVersion, hooks?, onChange, now? })` → `sync`, `push`, `ready`, `enqueue(ops, { holdMs? })`, `discard`, `retry`, `reindex`, `wipe`, `doc`, `docs`, `parked`, `status`.
- `types`: `AggregateModule`, `Feed`, `FeedPage`, `IndexSpec`, `OpBase`, `DocState`, `ChangeEvent`, `ParkedOp`.
- Kernel tables `docs` (a stable read surface: apps join it from their index tables), `outbox`, `cursors`, `meta`; every write folds the server doc through the queued ops (`local`), rewrites the module's index rows and reports the change after commit.
- Pull: paged feeds per module in order, resumable full syncs by generation, prune of unmentioned docs without queued ops.
- Push: single-flight drain with one queued rerun, head-of-line hold per hold key, backoff, park, and a 401 pause until the next sync; a timer replays held and backed-off ops once due.
- `status`: framework-free store for `useSyncExternalStore`; `bannerState`: the sync banner derived from it.
- `expo/triggers`: `startSyncTriggers(engine, { backgroundTaskName })` and `defineSyncTask(name, engine, prepare?)`; `prepare` runs before the background sync.
