import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import type { DocKey } from './docStore.ts';
import type { OpBase, ParkedOp } from './types.ts';

export interface OutboxRow {
  seq: number;
  op: OpBase;
  attempts: number;
}

export interface Failure {
  park: boolean;
  attempts: number;
  nextAttemptAt: number;
  lastStatus: number | null;
  lastError: string;
}

type RawRow = { seq: number; op: string; attempts: number; last_status: number | null; last_error: string | null };

export async function insertOp(tx: Tx, op: OpBase, holdKey: string, nextAttemptAt: number): Promise<void> {
  await tx.run(
    `INSERT INTO outbox (command_id, aggregate, aggregate_id, hold_key, kind, op, next_attempt_at)
     VALUES (?, ?, ?, ?, ?, ?, ?)`,
    [op.commandId, op.aggregate, op.aggregateId, holdKey, op.kind, JSON.stringify(op), nextAttemptAt],
  );
}

/** The oldest due pending op that is first in line for its hold key: an earlier op with the same key, parked or
 *  backed off, holds it so a failed create can't be overtaken by its edits. */
export async function nextEligible(tx: Tx, now: number): Promise<OutboxRow | null> {
  const row = await tx.first<RawRow>(
    `SELECT o.seq, o.op, o.attempts FROM outbox o
     WHERE o.status = 'pending' AND o.next_attempt_at <= ?
       AND NOT EXISTS (SELECT 1 FROM outbox h WHERE h.hold_key = o.hold_key AND h.seq < o.seq)
     ORDER BY o.seq LIMIT 1`,
    [now],
  );
  return row && { seq: row.seq, op: JSON.parse(row.op) as OpBase, attempts: row.attempts };
}

export async function opsFor(tx: Tx, aggregate: string, id: string): Promise<OpBase[]> {
  const rows = await tx.all<{ op: string }>(
    'SELECT op FROM outbox WHERE aggregate = ? AND aggregate_id = ? ORDER BY seq', [aggregate, id]);
  return rows.map((r) => JSON.parse(r.op) as OpBase);
}

export async function findOp(tx: Tx, commandId: string): Promise<OpBase | null> {
  const row = await tx.first<{ op: string }>('SELECT op FROM outbox WHERE command_id = ?', [commandId]);
  return row && (JSON.parse(row.op) as OpBase);
}

export async function deleteOp(tx: Tx, commandId: string): Promise<void> {
  await tx.run('DELETE FROM outbox WHERE command_id = ?', [commandId]);
}

export async function markFailure(tx: Tx, seq: number, f: Failure): Promise<void> {
  await tx.run(
    'UPDATE outbox SET status = ?, attempts = ?, next_attempt_at = ?, last_status = ?, last_error = ? WHERE seq = ?',
    [f.park ? 'parked' : 'pending', f.attempts, f.nextAttemptAt, f.lastStatus, f.lastError, seq],
  );
}

export async function requeue(tx: Tx, commandId: string): Promise<void> {
  await tx.run(
    `UPDATE outbox SET status = 'pending', attempts = 0, next_attempt_at = 0, last_status = NULL, last_error = NULL
     WHERE command_id = ?`,
    [commandId],
  );
}

export async function counts(tx: Tx): Promise<{ pending: number; parked: number }> {
  const rows = await tx.all<{ status: string; n: number }>('SELECT status, COUNT(*) AS n FROM outbox GROUP BY status');
  return {
    pending: rows.find((r) => r.status === 'pending')?.n ?? 0,
    parked: rows.find((r) => r.status === 'parked')?.n ?? 0,
  };
}

export async function listParked(tx: Tx): Promise<ParkedOp[]> {
  const rows = await tx.all<RawRow>(
    "SELECT seq, op, attempts, last_status, last_error FROM outbox WHERE status = 'parked' ORDER BY seq");
  return rows.map((r) => ({ op: JSON.parse(r.op) as OpBase, attempts: r.attempts, lastStatus: r.last_status, lastError: r.last_error }));
}

export async function queuedKeys(tx: Tx): Promise<DocKey[]> {
  return tx.all<DocKey>('SELECT DISTINCT aggregate, aggregate_id AS id FROM outbox');
}

export async function clearOutbox(tx: Tx): Promise<void> {
  await tx.run('DELETE FROM outbox');
}
