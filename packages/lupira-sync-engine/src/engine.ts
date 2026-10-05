import { migrate } from '@danbro96/lupira-expo-sqlite/migrate';
import type { Db } from '@danbro96/lupira-expo-sqlite/types';
import { classifyReplayError } from '@danbro96/lupira-sync-core/replayError';
import { getLocal, listLocal } from './docStore.ts';
import { ensureIndex, rebuildIndex } from './indexes.ts';
import { commit, moduleFor, refreshCounts, type Kernel } from './kernel.ts';
import { CACHE_VERSION_KEY, getMeta, setMeta } from './metaStore.ts';
import { deleteOp, findOp, insertOp, listParked, requeue } from './outboxStore.ts';
import { pullAll } from './pull.ts';
import { createPusher } from './push.ts';
import { recompute } from './recompute.ts';
import { dropCache, wipeAll } from './reset.ts';
import { KERNEL_MIGRATIONS } from './schema.ts';
import { createSerial } from './serial.ts';
import { createStatusStore, type StatusStore, type StatusWriter } from './status.ts';
import type { AggregateModule, ChangeEvent, DocState, OpBase, ParkedOp } from './types.ts';

export interface SyncHooks {
  beforePush?(): Promise<void>;
  afterPull?(): Promise<void>;
}

export interface SyncEngineOptions {
  openDb: () => Promise<Db>;
  /** Pulled in this order. */
  modules: AggregateModule[];
  /** Bump when a doc's stored shape changes: pulled data is dropped and resynced, queued ops are kept. */
  cacheVersion: number;
  hooks?: SyncHooks;
  onChange: (event: ChangeEvent) => void;
  now?: () => number;
}

export interface EnqueueOptions {
  /** Keeps the op queued (and discardable) this long before it replays. */
  holdMs?: number;
}

export interface SyncEngine {
  /** Push, then pull every module; concurrent calls share one run. Failures land in `status`, never reject. */
  sync(): Promise<void>;
  push(): Promise<void>;
  enqueue(ops: OpBase | readonly OpBase[], options?: EnqueueOptions): Promise<void>;
  discard(commandId: string): Promise<void>;
  retry(commandId: string): Promise<void>;
  reindex(aggregate: string): Promise<void>;
  wipe(): Promise<void>;
  doc<Doc, Guards>(aggregate: string, id: string): Promise<DocState<Doc, Guards> | null>;
  docs<Doc, Guards>(aggregate: string): Promise<{ id: string; state: DocState<Doc, Guards> }[]>;
  parked(): Promise<ParkedOp[]>;
  status: StatusStore;
}

export function createSyncEngine(options: SyncEngineOptions): SyncEngine {
  const status = createStatusStore();
  const exchange = createSerial();
  const pusher = createPusher(exchange);
  let opening: Promise<Kernel> | null = null;
  let syncing: Promise<void> | null = null;

  const kernel = (): Promise<Kernel> =>
    (opening ??= open(options, status).catch((e: unknown) => {
      opening = null;
      throw e;
    }));

  const kick = (k: Kernel): void => {
    pusher.drain(k).catch((e: unknown) => status.set({ lastError: messageOf(e) }));
  };

  async function runSync(): Promise<void> {
    try {
      const k = await kernel();
      pusher.resume();
      status.set({ phase: 'push' });
      await options.hooks?.beforePush?.();
      await pusher.drain(k);
      status.set({ phase: 'pull' });
      await pullAll(k, exchange);
      await options.hooks?.afterPull?.();
      status.set({ serverReachable: true, lastError: null, lastSyncAt: k.now() });
    } catch (e) {
      status.set({ lastError: messageOf(e), serverReachable: classifyReplayError(e).outcome !== 'retry' });
    } finally {
      status.set({ phase: 'idle', progress: null });
    }
  }

  return {
    sync: () =>
      (syncing ??= runSync().finally(() => {
        syncing = null;
      })),

    push: async () => pusher.drain(await kernel()),

    async enqueue(ops, { holdMs = 0 } = {}) {
      const k = await kernel();
      await commit(k, 'local', async (tx, changes) => {
        for (const op of [ops].flat()) {
          const module = moduleFor(k, op.aggregate);
          if (!module.reduce || !module.replay) throw new Error(`'${op.aggregate}' is read-only`);
          await insertOp(tx, op, module.holdKeyOf?.(op) ?? op.aggregateId, k.now() + holdMs);
          await recompute(tx, module, op.aggregateId, changes);
        }
      });
      await refreshCounts(k);
      kick(k);
    },

    async discard(commandId) {
      const k = await kernel();
      await commit(k, 'local', async (tx, changes) => {
        const op = await findOp(tx, commandId);
        if (!op) return;
        await deleteOp(tx, commandId);
        await recompute(tx, moduleFor(k, op.aggregate), op.aggregateId, changes);
      });
      await refreshCounts(k);
      kick(k);
    },

    async retry(commandId) {
      const k = await kernel();
      await k.db.exclusive((tx) => requeue(tx, commandId));
      await refreshCounts(k);
      kick(k);
    },

    async reindex(aggregate) {
      const k = await kernel();
      await commit(k, 'local', (tx, changes) => rebuildIndex(tx, moduleFor(k, aggregate), changes));
    },

    async wipe() {
      const k = await kernel();
      await commit(k, 'local', (tx, changes) => wipeAll(tx, k, changes));
      await refreshCounts(k);
    },

    doc: async <Doc, Guards>(aggregate: string, id: string) =>
      (await getLocal((await kernel()).db, aggregate, id)) as DocState<Doc, Guards> | null,

    docs: async <Doc, Guards>(aggregate: string) =>
      (await listLocal((await kernel()).db, aggregate)) as { id: string; state: DocState<Doc, Guards> }[],

    parked: async () => listParked((await kernel()).db),

    status,
  };
}

const messageOf = (e: unknown): string => (e instanceof Error ? e.message : String(e));

async function open(options: SyncEngineOptions, status: StatusWriter): Promise<Kernel> {
  const db = await options.openDb();
  await migrate(db, KERNEL_MIGRATIONS);
  const k: Kernel = { db, modules: options.modules, now: options.now ?? Date.now, onChange: options.onChange, status };
  for (const module of options.modules) if (module.index) await ensureIndex(k, module);
  await commit(k, 'local', async (tx, changes) => {
    const stored = await getMeta(tx, CACHE_VERSION_KEY);
    if (stored === String(options.cacheVersion)) return;
    if (stored !== null) await dropCache(tx, k, changes);
    await setMeta(tx, CACHE_VERSION_KEY, String(options.cacheVersion));
  });
  await refreshCounts(k);
  return k;
}
