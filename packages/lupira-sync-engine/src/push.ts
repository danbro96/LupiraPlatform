import type { Tx } from '@danbro96/lupira-expo-sqlite/types';
import { nextAttemptDelayMs, PARK_AFTER_ATTEMPTS } from '@danbro96/lupira-sync-core/backoff';
import { classifyReplayError, type ReplayDecision } from '@danbro96/lupira-sync-core/replayError';
import { getCursor } from './cursorStore.ts';
import { clearServer, getDoc, putServer } from './docStore.ts';
import { commit, moduleFor, refreshCounts, type ChangeSet, type Kernel } from './kernel.ts';
import { deleteOp, markFailure, nextEligible, type Failure, type OutboxRow } from './outboxStore.ts';
import { fold, recompute } from './recompute.ts';
import type { Serial } from './serial.ts';
import type { OpBase } from './types.ts';

export interface Pusher {
  drain(kernel: Kernel): Promise<void>;
  /** Lifts the pause a 401 put on draining. */
  resume(): void;
}

/** Single-flight: a drain requested mid-run queues exactly one rerun, so an op enqueued after the running drain's
 *  last check is not stranded until the next trigger. */
export function createPusher(exchange: Serial): Pusher {
  let running: Promise<void> | null = null;
  let rerun = false;
  let paused = false;

  async function runDrain(kernel: Kernel): Promise<void> {
    try {
      for (;;) {
        const row = await nextEligible(kernel.db, kernel.now());
        if (!row) return;
        const decision = await exchange(() => replayOne(kernel, row));
        if (decision?.outcome === 'pause') paused = true;
        if (decision?.stop) return;
      }
    } finally {
      await refreshCounts(kernel);
    }
  }

  function drain(kernel: Kernel): Promise<void> {
    if (paused) return running ?? Promise.resolve();
    if (running) {
      rerun = true;
      return running;
    }
    running = runDrain(kernel)
      .finally(() => {
        running = null;
      })
      .then(() => {
        if (!rerun) return;
        rerun = false;
        return drain(kernel);
      });
    return running;
  }

  return {
    drain,
    resume: () => {
      paused = false;
    },
  };
}

async function replayOne(kernel: Kernel, row: OutboxRow): Promise<ReplayDecision | null> {
  try {
    const module = moduleFor(kernel, row.op.aggregate);
    if (!module.replay) throw new Error(`'${module.aggregate}' is read-only`);
    await module.replay(row.op);
  } catch (e) {
    const decision = classifyReplayError(e);
    if (decision.outcome !== 'pause') await kernel.db.exclusive((tx) => markFailure(tx, row.seq, failureOf(kernel, row, decision, e)));
    kernel.status.set({ lastError: decision.reason, ...(decision.outcome === 'retry' && { serverReachable: false }) });
    return decision;
  }
  await commit(kernel, 'local', (tx, changes) => acknowledge(tx, kernel, row.op, changes));
  kernel.status.set({ serverReachable: true, lastError: null });
  return null;
}

/** The server applied the op, so it moves from the queue into the server base, where it stays visible until the
 *  next pull confirms it. Seen in the current generation, so an in-progress full sync won't prune it. */
async function acknowledge(tx: Tx, kernel: Kernel, op: OpBase, changes: ChangeSet): Promise<void> {
  const module = moduleFor(kernel, op.aggregate);
  const server = fold(module, (await getDoc(tx, op.aggregate, op.aggregateId))?.server ?? null, [op]);
  await deleteOp(tx, op.commandId);
  if (server) await putServer(tx, op.aggregate, op.aggregateId, server, (await getCursor(tx, op.aggregate)).fullGen);
  else await clearServer(tx, op.aggregate, op.aggregateId);
  await recompute(tx, module, op.aggregateId, changes);
}

function failureOf(kernel: Kernel, row: OutboxRow, decision: ReplayDecision, e: unknown): Failure {
  const attempts = row.attempts + 1;
  const park = decision.outcome === 'park' || attempts >= PARK_AFTER_ATTEMPTS;
  return {
    park,
    attempts,
    nextAttemptAt: park ? 0 : kernel.now() + nextAttemptDelayMs(attempts),
    lastStatus: statusOf(e),
    lastError: decision.reason,
  };
}

function statusOf(e: unknown): number | null {
  const status = e instanceof Error ? (e as { status?: unknown }).status : undefined;
  return typeof status === 'number' ? status : null;
}
