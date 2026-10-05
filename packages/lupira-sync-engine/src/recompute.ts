import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import { getDoc, putLocal, removeDoc } from './docStore.ts';
import type { ChangeSet } from './kernel.ts';
import { opsFor } from './outboxStore.ts';
import type { AggregateModule, DocState, OpBase } from './types.ts';

export function fold(module: AggregateModule, start: DocState | null, ops: OpBase[]): DocState | null {
  let state = start;
  for (const op of ops) state = module.reduce ? module.reduce(state, op) : state;
  return state;
}

/** local = server folded through every queued op (parked included: still the user's intent until discarded). */
export async function recompute(tx: Tx, module: AggregateModule, id: string, changes: ChangeSet): Promise<void> {
  const row = await getDoc(tx, module.aggregate, id);
  const server = row?.server ?? null;
  const ops = await opsFor(tx, module.aggregate, id);
  const local = fold(module, server, ops);

  if (server === null && local === null && ops.length === 0) await removeDoc(tx, module.aggregate, id);
  else await putLocal(tx, module.aggregate, id, local);

  if (JSON.stringify(row?.local ?? null) === JSON.stringify(local)) return;
  await module.index?.write(tx, id, local);
  changes.add(module.aggregate, id);
}
