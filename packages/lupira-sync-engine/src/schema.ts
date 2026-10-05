export const KERNEL_MIGRATIONS: readonly string[] = [
  `CREATE TABLE docs (
     aggregate TEXT NOT NULL,
     id TEXT NOT NULL,
     server TEXT,
     local TEXT,
     seen_gen INTEGER NOT NULL DEFAULT 0,
     PRIMARY KEY (aggregate, id)
   );
   CREATE TABLE outbox (
     seq INTEGER PRIMARY KEY AUTOINCREMENT,
     command_id TEXT NOT NULL UNIQUE,
     aggregate TEXT NOT NULL,
     aggregate_id TEXT NOT NULL,
     hold_key TEXT NOT NULL,
     kind TEXT NOT NULL,
     op TEXT NOT NULL,
     status TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'parked')),
     attempts INTEGER NOT NULL DEFAULT 0,
     next_attempt_at INTEGER NOT NULL DEFAULT 0,
     last_status INTEGER,
     last_error TEXT
   );
   CREATE INDEX outbox_due ON outbox (status, next_attempt_at, seq);
   CREATE INDEX outbox_aggregate ON outbox (aggregate, aggregate_id, seq);
   CREATE INDEX outbox_hold ON outbox (hold_key, seq);
   CREATE TABLE cursors (
     aggregate TEXT PRIMARY KEY,
     cursor TEXT,
     full_gen INTEGER NOT NULL DEFAULT 0,
     full_done INTEGER NOT NULL DEFAULT 0
   );
   CREATE TABLE meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);`,
];
