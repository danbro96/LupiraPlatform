import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import { clearCursors } from './cursorStore.ts';
import { allKeys, clearDocs } from './docStore.ts';
import { clearIndexes } from './indexes.ts';
import type { ChangeSet, Kernel } from './kernel.ts';
import { clearOutbox, queuedKeys } from './outboxStore.ts';
import { recompute } from './recompute.ts';

/** Forgets everything pulled so the next sync starts over; queued ops survive and stay visible. */
export async function dropCache(tx: Tx, kernel: Kernel, changes: ChangeSet): Promise<void> {
  await clearPulled(tx, kernel, changes);
  for (const key of await queuedKeys(tx)) {
    const module = kernel.modules.find((m) => m.aggregate === key.aggregate);
    if (module) await recompute(tx, module, key.id, changes);
  }
}

export async function wipeAll(tx: Tx, kernel: Kernel, changes: ChangeSet): Promise<void> {
  await clearOutbox(tx);
  await clearPulled(tx, kernel, changes);
}

async function clearPulled(tx: Tx, kernel: Kernel, changes: ChangeSet): Promise<void> {
  for (const key of await allKeys(tx)) changes.add(key.aggregate, key.id);
  await clearDocs(tx);
  await clearCursors(tx);
  await clearIndexes(tx, kernel.modules);
}
