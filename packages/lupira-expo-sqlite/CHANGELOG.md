# Changelog

## 0.1.0

- `types`: `Db`, `Tx`, `SqlValue`.
- `expoDb`: `expoDb(name)` → shared lazily-opened `Db` (WAL, busy timeout, exclusive transactions).
- `migrate`: `migrate(db, migrations)` over `PRAGMA user_version`.
- `node`: `openNodeDb(path?)` for vitest; never imported from app code.
