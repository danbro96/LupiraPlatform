import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import type { DocState } from './types.ts';

export interface DocRow {
  server: DocState | null;
  local: DocState | null;
}

export interface DocKey {
  aggregate: string;
  id: string;
}

type RawRow = { server: string | null; local: string | null };

const parse = (json: string | null): DocState | null => (json === null ? null : (JSON.parse(json) as DocState));

export async function getDoc(tx: Tx, aggregate: string, id: string): Promise<DocRow | null> {
  const row = await tx.first<RawRow>('SELECT server, local FROM docs WHERE aggregate = ? AND id = ?', [aggregate, id]);
  return row && { server: parse(row.server), local: parse(row.local) };
}

export async function getLocal(tx: Tx, aggregate: string, id: string): Promise<DocState | null> {
  return (await getDoc(tx, aggregate, id))?.local ?? null;
}

export async function listLocal(tx: Tx, aggregate: string): Promise<{ id: string; state: DocState }[]> {
  const rows = await tx.all<{ id: string; local: string }>(
    'SELECT id, local FROM docs WHERE aggregate = ? AND local IS NOT NULL ORDER BY id', [aggregate]);
  return rows.map((r) => ({ id: r.id, state: JSON.parse(r.local) as DocState }));
}

export async function putServer(tx: Tx, aggregate: string, id: string, server: DocState, seenGen: number): Promise<void> {
  await tx.run(
    `INSERT INTO docs (aggregate, id, server, seen_gen) VALUES (?, ?, ?, ?)
     ON CONFLICT (aggregate, id) DO UPDATE SET server = excluded.server, seen_gen = excluded.seen_gen`,
    [aggregate, id, JSON.stringify(server), seenGen],
  );
}

export async function clearServer(tx: Tx, aggregate: string, id: string): Promise<void> {
  await tx.run('UPDATE docs SET server = NULL WHERE aggregate = ? AND id = ?', [aggregate, id]);
}

export async function putLocal(tx: Tx, aggregate: string, id: string, local: DocState | null): Promise<void> {
  await tx.run(
    `INSERT INTO docs (aggregate, id, local) VALUES (?, ?, ?)
     ON CONFLICT (aggregate, id) DO UPDATE SET local = excluded.local`,
    [aggregate, id, local === null ? null : JSON.stringify(local)],
  );
}

export async function removeDoc(tx: Tx, aggregate: string, id: string): Promise<void> {
  await tx.run('DELETE FROM docs WHERE aggregate = ? AND id = ?', [aggregate, id]);
}

/** Server-backed docs a full sync of generation `gen` never mentioned and no queued op still needs. */
export async function unseenIds(tx: Tx, aggregate: string, gen: number): Promise<string[]> {
  const rows = await tx.all<{ id: string }>(
    `SELECT d.id FROM docs d
     WHERE d.aggregate = ? AND d.seen_gen < ? AND d.server IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM outbox o WHERE o.aggregate = d.aggregate AND o.aggregate_id = d.id)`,
    [aggregate, gen],
  );
  return rows.map((r) => r.id);
}

export async function allKeys(tx: Tx): Promise<DocKey[]> {
  return tx.all<DocKey>('SELECT aggregate, id FROM docs');
}

export async function clearDocs(tx: Tx): Promise<void> {
  await tx.run('DELETE FROM docs');
}
