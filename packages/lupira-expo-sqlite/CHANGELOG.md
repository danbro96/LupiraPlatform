# Changelog

## 0.4.1

- `applySchema(db, sql)`: runs DDL in one `BEGIN IMMEDIATE` … `COMMIT` on the main connection, rolling back on failure, so reads on that connection always see the new schema.
- `migrate`: each step runs through `applySchema` instead of `exclusive`; a fresh install no longer fails with `no such table` when the main connection held an open read.
- `node`: `Db.exec` waits for an open `exclusive` transaction, as the device's main connection waits for the write lock.

## 0.4.0

- `migrate`: each step's DDL and `PRAGMA user_version` run in one exclusive transaction and commit together; a failing step leaves no partial schema.
- `types`: `exec` moves from `Db` to `Tx` (still on `Db`), so DDL can run inside `exclusive`.

## 0.3.0

- `exclusive` opens its own connection and takes the write lock with `BEGIN IMMEDIATE`, so the busy timeout applies to every transaction; the deferred `BEGIN` failed instantly (`database is locked`) when another connection committed between its read and its write.

## 0.2.0

- `expoDb(name, options?)`: every native call retries once when expo-modules-core reports a live statement as already released; `onRetry(message)` observes it.
- `serializeStatements` option: runs non-transaction statements through one FIFO gate (default off); exclusive transactions keep their own connection.

## 0.1.0

- `types`: `Db`, `Tx`, `SqlValue`.
- `expoDb`: `expoDb(name)` → shared lazily-opened `Db` (WAL, busy timeout, exclusive transactions).
- `migrate`: `migrate(db, migrations)` over `PRAGMA user_version`.
- `node`: `openNodeDb(path?)` for vitest; never imported from app code.
