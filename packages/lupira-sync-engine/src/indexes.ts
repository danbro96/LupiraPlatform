import { applySchema } from '@danbro96/lupira-expo-sqlite/applySchema';
import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import { listLocal } from './docStore.ts';
import { commit, type Kernel } from './kernel.ts';
import { deleteMeta, getMeta, indexVersionKey, setMeta } from './metaStore.ts';
import type { AggregateModule } from './types.ts';

/** Recreates the module's index tables from its DDL and rewrites every visible doc into them. */
export async function rebuildIndex(kernel: Kernel, module: AggregateModule): Promise<void> {
  const index = module.index;
  if (!index) return;
  const versionKey = indexVersionKey(module.aggregate);
  // The version is recorded only once the rewrite commits, so a rebuild cut short is redone on the next open.
  await kernel.db.exclusive((tx) => deleteMeta(tx, versionKey));
  await applySchema(kernel.db, [...index.tables.map((table) => `DROP TABLE IF EXISTS ${table};`), index.ddl].join('\n'));
  await commit(kernel, 'local', async (tx, changes) => {
    for (const { id, state } of await listLocal(tx, module.aggregate)) {
      await index.write(tx, id, state);
      changes.add(module.aggregate, id);
    }
    await setMeta(tx, versionKey, String(index.version));
  });
}

export async function ensureIndex(kernel: Kernel, module: AggregateModule): Promise<void> {
  if ((await getMeta(kernel.db, indexVersionKey(module.aggregate))) === String(module.index?.version)) return;
  await rebuildIndex(kernel, module);
}

export async function clearIndexes(tx: Tx, modules: AggregateModule[]): Promise<void> {
  for (const module of modules) for (const table of module.index?.tables ?? []) await tx.run(`DELETE FROM ${table}`);
}
