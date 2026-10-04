import * as SQLite from 'expo-sqlite';
import type { Db, SqlValue, Tx } from './types.ts';

export interface ExpoDbOptions {
  onRetry?: (message: string) => void;
  /** Runs every non-transaction statement through one FIFO gate, so at most one native statement is in flight. */
  serializeStatements?: boolean;
}

type Guard = <T>(fn: () => Promise<T>) => Promise<T>;

export function expoDb(name: string, options: ExpoDbOptions = {}): () => Promise<Db> {
  let dbPromise: Promise<Db> | null = null;
  return () => {
    dbPromise ??= open(name, options);
    return dbPromise;
  };
}

async function open(name: string, { onRetry, serializeStatements = false }: ExpoDbOptions): Promise<Db> {
  const raw = await SQLite.openDatabaseAsync(name);
  const retrying: Guard = (fn) => withRetry(fn, onRetry);
  const guard: Guard = serializeStatements ? gated(retrying) : retrying;
  // busy_timeout: exclusive transactions ride a second connection (expo-sqlite), and a native bridge may read
  // this file too — waiting beats an instant "database is locked" during the first big pull.
  await guard(() => raw.execAsync('PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 30000;'));
  return wrap(raw, guard, retrying);
}

// expo-modules-core's shared-object registry can report a live NativeStatement as released while argument
// conversion runs; that fails before any SQL executes, so re-issuing is safe even mid-transaction.
const RELEASED_SHARED_OBJECT = /already released/i;

function isReleasedSharedObject(e: unknown): boolean {
  for (let x: unknown = e, depth = 0; x instanceof Error && depth < 5; x = x.cause, depth++) {
    if (RELEASED_SHARED_OBJECT.test(x.message)) return true;
  }
  return false;
}

async function withRetry<T>(fn: () => Promise<T>, onRetry?: (message: string) => void): Promise<T> {
  try {
    return await fn();
  } catch (e) {
    if (!isReleasedSharedObject(e)) throw e;
    onRetry?.(e instanceof Error ? e.message.split('\n')[0] : String(e));
  }
  return fn();
}

function gated(inner: Guard): Guard {
  let gate: Promise<unknown> = Promise.resolve();
  return (fn) => {
    const run = gate.then(() => inner(fn));
    gate = run.catch(() => {});
    return run;
  };
}

function wrapTx(h: SQLite.SQLiteDatabase, guard: Guard): Tx {
  return {
    run: async (sql, params = []) => {
      await guard(() => h.runAsync(sql, params as SQLite.SQLiteBindParams));
    },
    all: async <T,>(sql: string, params: SqlValue[] = []) => guard(() => h.getAllAsync<T>(sql, params as SQLite.SQLiteBindParams)),
    first: async <T,>(sql: string, params: SqlValue[] = []) => guard(() => h.getFirstAsync<T>(sql, params as SQLite.SQLiteBindParams)),
  };
}

function wrap(raw: SQLite.SQLiteDatabase, guard: Guard, retrying: Guard): Db {
  return {
    ...wrapTx(raw, guard),
    exec: (sql) => guard(() => raw.execAsync(sql)),
    // withExclusiveTransactionAsync hands us a dedicated handle no other query can interleave with — the
    // whole point of the Tx-threading discipline (see types.ts). It returns void, so the result rides a closure.
    async exclusive<T>(fn: (tx: Tx) => Promise<T>): Promise<T> {
      let result!: T;
      await raw.withExclusiveTransactionAsync(async (txn) => {
        const tx = wrapTx(txn as unknown as SQLite.SQLiteDatabase, retrying);
        // busy_timeout is per-connection and this txn rides its own — cover commits/escalations
        // against other connections instead of failing instantly.
        await tx.first('PRAGMA busy_timeout = 30000');
        result = await fn(tx);
      });
      return result;
    },
  };
}
