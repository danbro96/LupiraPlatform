import { migrate } from '@danbro96/lupira-expo-sqlite/migrate';
import type { Db } from '@danbro96/lupira-expo-sqlite/types';
import { classifyReplayError, describeFailure } from '@danbro96/lupira-sync-core/replayError';
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
import type { AggregateModule, ChangeEvent, DocState, OpBase, ParkedOp, SyncFailure, SyncStep } from './types.ts';

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
  onFailure?: (failure: SyncFailure) => void;
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
  /** Resolves once the database is migrated and every module's index tables exist; reads should await it. */
  ready(): Promise<void>;
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

  async function runSync(): Promise<void> {
    let step: SyncStep = 'open';
    try {
      const k = await kernel();
      pusher.resume();
      status.set({ phase: 'push' });
      step = 'beforePush';
      await options.hooks?.beforePush?.();
      step = 'push';
      await pusher.drain(k);
      status.set({ phase: 'pull' });
      step = 'pull';
      await pullAll(k, exchange);
      step = 'afterPull';
      await options.hooks?.afterPull?.();
      status.set({ serverReachable: true, lastError: null, lastSyncAt: k.now() });
    } catch (e) {
      const decision = classifyReplayError(e);
      status.set({ lastError: describeFailure(decision), serverReachable: decision.outcome !== 'retry' });
      options.onFailure?.({ phase: step, decision, error: e });
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

    ready: async () => {
      await kernel();
    },

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
      pusher.kick(k);
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
      pusher.kick(k);
    },

    async retry(commandId) {
      const k = await kernel();
      await k.db.exclusive((tx) => requeue(tx, commandId));
      await refreshCounts(k);
      pusher.kick(k);
    },

    async reindex(aggregate) {
      const k = await kernel();
      await rebuildIndex(k, moduleFor(k, aggregate));
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

async function open(options: SyncEngineOptions, status: StatusWriter): Promise<Kernel> {
  const db = await options.openDb();
  await migrate(db, KERNEL_MIGRATIONS);
  const k: Kernel = {
    db,
    modules: options.modules,
    now: options.now ?? Date.now,
    onChange: options.onChange,
    onFailure: options.onFailure,
    status,
  };
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
