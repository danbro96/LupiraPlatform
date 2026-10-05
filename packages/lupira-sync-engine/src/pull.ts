import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import { getCursor, saveCursor } from './cursorStore.ts';
import { clearServer, putServer, unseenIds } from './docStore.ts';
import { commit, type ChangeSet, type Kernel } from './kernel.ts';
import { recompute } from './recompute.ts';
import type { Serial } from './serial.ts';
import type { AggregateModule, FeedPage } from './types.ts';

export async function pullAll(kernel: Kernel, exchange: Serial): Promise<void> {
  for (const module of kernel.modules) await pullModule(kernel, exchange, module);
}

async function pullModule(kernel: Kernel, exchange: Serial, module: AggregateModule): Promise<void> {
  let since = (await getCursor(kernel.db, module.aggregate)).cursor;
  let count = 0;
  kernel.status.set({ progress: { aggregate: module.aggregate, count } });
  for (let hasMore = true; hasMore; ) {
    const page = await exchange(async () => {
      const fetched = await module.feed.fetch(since);
      await commit(kernel, 'pull', (tx, changes) => applyPage(tx, module, since, fetched, changes));
      return fetched;
    });
    since = page.cursor;
    hasMore = page.hasMore;
    count += page.changed.length + page.deleted.length;
    kernel.status.set({ progress: { aggregate: module.aggregate, count } });
  }
}

/** A null `since` or a `reset` page starts a new full-sync generation; its last page prunes what the stream never
 *  mentioned. The cursor is stored with every page, so an interrupted full sync resumes in its own generation. */
async function applyPage(tx: Tx, module: AggregateModule, since: string | null, page: FeedPage<unknown>, changes: ChangeSet): Promise<void> {
  const stored = await getCursor(tx, module.aggregate);
  const startsFull = since === null || page.reset;
  const gen = startsFull ? stored.fullGen + 1 : stored.fullGen;
  const inFull = startsFull || !stored.fullDone;

  for (const change of page.changed) {
    const { id, state } = module.feed.fromWire(change);
    await putServer(tx, module.aggregate, id, state, gen);
    await recompute(tx, module, id, changes);
  }
  for (const id of page.deleted) {
    await clearServer(tx, module.aggregate, id);
    await recompute(tx, module, id, changes);
  }
  if (inFull && !page.hasMore) await prune(tx, module, gen, changes);
  await saveCursor(tx, module.aggregate, { cursor: page.cursor, fullGen: gen, fullDone: !inFull || !page.hasMore });
}

async function prune(tx: Tx, module: AggregateModule, gen: number, changes: ChangeSet): Promise<void> {
  for (const id of await unseenIds(tx, module.aggregate, gen)) {
    await clearServer(tx, module.aggregate, id);
    await recompute(tx, module, id, changes);
  }
}
