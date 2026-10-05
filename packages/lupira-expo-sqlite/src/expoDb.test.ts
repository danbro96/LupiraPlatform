import { beforeEach, describe, expect, it, vi } from 'vitest';

const handle = vi.hoisted(() => ({
  execAsync: vi.fn(),
  runAsync: vi.fn(),
  getAllAsync: vi.fn(),
  getFirstAsync: vi.fn(),
  closeAsync: vi.fn(),
}));
const openDatabaseAsync = vi.hoisted(() => vi.fn());
vi.mock('expo-sqlite', () => ({ openDatabaseAsync }));

import { expoDb } from './expoDb.ts';

const released = () => new Error('Call to function NativeStatement.run has been rejected.\nShared object already released');

beforeEach(() => {
  for (const fn of Object.values(handle)) fn.mockReset();
  handle.execAsync.mockResolvedValue(undefined);
  handle.getAllAsync.mockResolvedValue([]);
  handle.getFirstAsync.mockResolvedValue(null);
  handle.runAsync.mockResolvedValue(undefined);
  handle.closeAsync.mockResolvedValue(undefined);
  openDatabaseAsync.mockReset();
  openDatabaseAsync.mockImplementation(async () => handle);
});

describe('shared-object retry', () => {
  it('re-issues a statement once and reports it', async () => {
    const onRetry = vi.fn();
    const db = await expoDb('t', { onRetry })();
    handle.getAllAsync.mockRejectedValueOnce(released()).mockResolvedValueOnce([{ id: 'a' }]);
    expect(await db.all('SELECT id FROM t')).toEqual([{ id: 'a' }]);
    expect(onRetry).toHaveBeenCalledWith('Call to function NativeStatement.run has been rejected.');
  });

  it('finds the failure through the cause chain', async () => {
    const db = await expoDb('t')();
    handle.runAsync.mockRejectedValueOnce(new Error('wrapped', { cause: released() }));
    await db.run('DELETE FROM t');
    expect(handle.runAsync).toHaveBeenCalledTimes(2);
  });

  it('gives up after one retry', async () => {
    const db = await expoDb('t')();
    handle.getFirstAsync.mockRejectedValue(released());
    await expect(db.first('SELECT 1')).rejects.toThrow('already released');
    expect(handle.getFirstAsync).toHaveBeenCalledTimes(2);
  });

  it('does not retry anything else', async () => {
    const onRetry = vi.fn();
    const db = await expoDb('t', { onRetry })();
    handle.runAsync.mockRejectedValue(new Error('constraint failed'));
    await expect(db.run('INSERT INTO t VALUES (1)')).rejects.toThrow('constraint failed');
    expect(handle.runAsync).toHaveBeenCalledTimes(1);
    expect(onRetry).not.toHaveBeenCalled();
  });

  it('covers statements inside an exclusive transaction', async () => {
    const db = await expoDb('t')();
    handle.runAsync.mockRejectedValueOnce(released());
    await db.exclusive(async (tx) => tx.run('INSERT INTO t VALUES (1)'));
    expect(handle.runAsync).toHaveBeenCalledTimes(2);
  });
});

describe('serializeStatements', () => {
  const inFlightPeak = async (options: { serializeStatements: boolean }) => {
    const db = await expoDb('t', options)();
    let inFlight = 0;
    let peak = 0;
    handle.getAllAsync.mockImplementation(async () => {
      peak = Math.max(peak, ++inFlight);
      await new Promise((r) => setTimeout(r, 2));
      inFlight--;
      return [];
    });
    await Promise.all([db.all('SELECT 1'), db.all('SELECT 2'), db.all('SELECT 3')]);
    return peak;
  };

  it('keeps one statement in flight when on', async () => {
    expect(await inFlightPeak({ serializeStatements: true })).toBe(1);
  });

  it('lets statements overlap by default', async () => {
    expect(await inFlightPeak({ serializeStatements: false })).toBeGreaterThan(1);
  });

  it('does not let a failed statement poison the gate', async () => {
    const db = await expoDb('t', { serializeStatements: true })();
    handle.runAsync.mockRejectedValueOnce(new Error('boom'));
    const failed = db.run('INSERT INTO t VALUES (1)');
    const next = db.all('SELECT 1');
    await expect(failed).rejects.toThrow('boom');
    await expect(next).resolves.toEqual([]);
  });

  it('keeps call order', async () => {
    const db = await expoDb('t', { serializeStatements: true })();
    const order: string[] = [];
    handle.runAsync.mockImplementation(async (sql: string) => {
      order.push(sql);
    });
    await Promise.all([db.run('a'), db.run('b'), db.run('c')]);
    expect(order).toEqual(['a', 'b', 'c']);
  });
});

describe('exclusive', () => {
  const statements = () => handle.execAsync.mock.calls.map(([sql]) => sql as string);

  it('opens its own connection and takes the write lock before running', async () => {
    const db = await expoDb('t')();
    handle.execAsync.mockClear();
    openDatabaseAsync.mockClear();
    await db.exclusive(async () => undefined);
    expect(openDatabaseAsync).toHaveBeenCalledWith('t', { useNewConnection: true });
    expect(statements()).toEqual(['PRAGMA busy_timeout = 30000; BEGIN IMMEDIATE', 'COMMIT']);
    expect(handle.closeAsync).toHaveBeenCalledTimes(1);
  });

  it('returns the callback result', async () => {
    const db = await expoDb('t')();
    expect(await db.exclusive(async () => 42)).toBe(42);
  });

  it('rolls back, closes and rethrows the original error', async () => {
    const db = await expoDb('t')();
    handle.execAsync.mockClear();
    await expect(db.exclusive(async () => { throw new Error('boom'); })).rejects.toThrow('boom');
    expect(statements()).toEqual(['PRAGMA busy_timeout = 30000; BEGIN IMMEDIATE', 'ROLLBACK']);
    expect(handle.closeAsync).toHaveBeenCalledTimes(1);
  });

  it('surfaces the original error when the rollback itself fails', async () => {
    const db = await expoDb('t')();
    handle.execAsync.mockImplementation(async (sql: string) => {
      if (sql === 'ROLLBACK') throw new Error('rollback failed');
    });
    await expect(db.exclusive(async () => { throw new Error('boom'); })).rejects.toThrow('boom');
  });

  it('closes the connection when taking the lock fails', async () => {
    const db = await expoDb('t')();
    handle.execAsync.mockRejectedValueOnce(new Error('database is locked'));
    await expect(db.exclusive(async () => undefined)).rejects.toThrow('database is locked');
    expect(handle.closeAsync).toHaveBeenCalledTimes(1);
  });
});
