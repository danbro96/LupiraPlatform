import type { Db } from './types.ts';

// Single-flight per db handle: two startup paths migrating at once on a virgin database both read
// user_version 0 and the loser hits "table already exists".
const migrating = new WeakMap<Db, Promise<void>>();

/** Runs an append-only migration ladder keyed on PRAGMA user_version: each entry runs once, in order. New schema
 *  work = push another migration; never edit a shipped entry. */
export function migrate(db: Db, migrations: readonly string[]): Promise<void> {
  let inFlight = migrating.get(db);
  if (!inFlight) {
    inFlight = runMigrate(db, migrations).finally(() => migrating.delete(db));
    migrating.set(db, inFlight);
  }
  return inFlight;
}

async function runMigrate(db: Db, migrations: readonly string[]): Promise<void> {
  const row = await db.first<{ user_version: number }>('PRAGMA user_version');
  const from = row?.user_version ?? 0;
  for (let v = from; v < migrations.length; v++) {
    // PRAGMA can't be parameterized and user_version must commit WITH the DDL, so both run via exec
    // inside one exclusive scope per step.
    await db.exclusive(async () => undefined);   // drain writers before DDL
    await db.exec(`${migrations[v]}\nPRAGMA user_version = ${v + 1};`);
  }
}
