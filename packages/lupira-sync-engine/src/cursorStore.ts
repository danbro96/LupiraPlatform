import type { Tx } from '@danbro96/lupira-expo-sqlite/types';

/** Where a feed resumes. `fullGen` numbers full syncs; `fullDone` is false while one is still paging. */
export interface CursorState {
  cursor: string | null;
  fullGen: number;
  fullDone: boolean;
}

export async function getCursor(tx: Tx, aggregate: string): Promise<CursorState> {
  const row = await tx.first<{ cursor: string | null; full_gen: number; full_done: number }>(
    'SELECT cursor, full_gen, full_done FROM cursors WHERE aggregate = ?', [aggregate]);
  return row ? { cursor: row.cursor, fullGen: row.full_gen, fullDone: row.full_done === 1 } : { cursor: null, fullGen: 0, fullDone: false };
}

export async function saveCursor(tx: Tx, aggregate: string, state: CursorState): Promise<void> {
  await tx.run(
    `INSERT INTO cursors (aggregate, cursor, full_gen, full_done) VALUES (?, ?, ?, ?)
     ON CONFLICT (aggregate) DO UPDATE SET cursor = excluded.cursor, full_gen = excluded.full_gen, full_done = excluded.full_done`,
    [aggregate, state.cursor, state.fullGen, state.fullDone ? 1 : 0],
  );
}

export async function clearCursors(tx: Tx): Promise<void> {
  await tx.run('DELETE FROM cursors');
}
