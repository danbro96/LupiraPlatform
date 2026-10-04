# Changelog

## 0.2.0

- `expoDb(name, options?)`: every native call retries once when expo-modules-core reports a live statement as already released; `onRetry(message)` observes it.
- `serializeStatements` option: runs non-transaction statements through one FIFO gate (default off); exclusive transactions keep their own connection.

## 0.1.0

- `types`: `Db`, `Tx`, `SqlValue`.
- `expoDb`: `expoDb(name)` → shared lazily-opened `Db` (WAL, busy timeout, exclusive transactions).
- `migrate`: `migrate(db, migrations)` over `PRAGMA user_version`.
- `node`: `openNodeDb(path?)` for vitest; never imported from app code.
