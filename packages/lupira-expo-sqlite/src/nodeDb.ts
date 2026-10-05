/// <reference types="node" />
/** node:sqlite adapter — the vitest harness. NEVER imported from app code (Metro must not see node:*);
 *  only test files construct it. Exclusive transactions are emulated with BEGIN IMMEDIATE plus a promise
 *  mutex, matching expo-sqlite's serialization semantics closely enough for engine tests. */
import { DatabaseSync } from 'node:sqlite';
import type { Db, SqlValue, Tx } from './types.ts';

export function openNodeDb(path = ':memory:'): Db {
  const raw = new DatabaseSync(path);
  raw.exec('PRAGMA journal_mode = WAL;');

  const tx: Tx = {
    run: async (sql, params = []) => {
      raw.prepare(sql).run(...params);
    },
    all: async <T,>(sql: string, params: SqlValue[] = []) => raw.prepare(sql).all(...params) as T[],
    first: async <T,>(sql: string, params: SqlValue[] = []) => (raw.prepare(sql).get(...params) as T | undefined) ?? null,
    exec: async (sql) => raw.exec(sql),
  };

  let queue: Promise<unknown> = Promise.resolve();

  return {
    ...tx,
    exclusive<T>(fn: (t: Tx) => Promise<T>): Promise<T> {
      const next = queue.then(async () => {
        raw.exec('BEGIN IMMEDIATE');
        try {
          const result = await fn(tx);
          raw.exec('COMMIT');
          return result;
        } catch (e) {
          raw.exec('ROLLBACK');
          throw e;
        }
      });
      queue = next.catch(() => undefined);
      return next;
    },
  };
}
