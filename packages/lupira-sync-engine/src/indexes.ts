import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import { listLocal } from './docStore.ts';
import { commit, type ChangeSet, type Kernel } from './kernel.ts';
import { getMeta, indexVersionKey, setMeta } from './metaStore.ts';
import type { AggregateModule } from './types.ts';

/** Recreates the module's index tables from its DDL and rewrites every visible doc into them. */
export async function rebuildIndex(tx: Tx, module: AggregateModule, changes: ChangeSet): Promise<void> {
  const index = module.index;
  if (!index) return;
  for (const table of index.tables) await tx.exec(`DROP TABLE IF EXISTS ${table};`);
  await tx.exec(index.ddl);
  for (const { id, state } of await listLocal(tx, module.aggregate)) {
    await index.write(tx, id, state);
    changes.add(module.aggregate, id);
  }
  await setMeta(tx, indexVersionKey(module.aggregate), String(index.version));
}

export async function ensureIndex(kernel: Kernel, module: AggregateModule): Promise<void> {
  await commit(kernel, 'local', async (tx, changes) => {
    if ((await getMeta(tx, indexVersionKey(module.aggregate))) === String(module.index?.version)) return;
    await rebuildIndex(tx, module, changes);
  });
}

export async function clearIndexes(tx: Tx, modules: AggregateModule[]): Promise<void> {
  for (const module of modules) for (const table of module.index?.tables ?? []) await tx.run(`DELETE FROM ${table}`);
}
