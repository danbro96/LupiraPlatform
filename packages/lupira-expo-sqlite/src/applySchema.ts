import type { Db } from './types.ts';

/** Runs DDL atomically on the main connection: a connection holding a read snapshot never sees tables another
 *  connection (such as `exclusive`'s) created after that snapshot opened. */
export async function applySchema(db: Db, sql: string): Promise<void> {
  try {
    await db.exec(`BEGIN IMMEDIATE;\n${sql}\n;\nCOMMIT;`);
  } catch (e) {
    await db.exec('ROLLBACK').catch(() => {});
    throw e;
  }
}
