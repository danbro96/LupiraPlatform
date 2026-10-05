import { describe, expect, it } from 'vitest';
import { migrate } from './migrate.ts';
import { openNodeDb } from './nodeDb.ts';

const LADDER = [
  'CREATE TABLE items (id TEXT PRIMARY KEY, title TEXT NOT NULL);',
  'ALTER TABLE items ADD COLUMN done INTEGER NOT NULL DEFAULT 0;',
];

describe('openNodeDb', () => {
  it('runs, reads and binds parameters', async () => {
    const db = openNodeDb();
    await db.exec('CREATE TABLE t (id TEXT PRIMARY KEY, n INTEGER, note TEXT);');
    await db.run('INSERT INTO t VALUES (?, ?, ?)', ['a', 1, null]);
    await db.run('INSERT INTO t VALUES (?, ?, ?)', ['b', 2, 'x']);
    expect(await db.all<{ id: string }>('SELECT id FROM t ORDER BY id')).toEqual([{ id: 'a' }, { id: 'b' }]);
    expect(await db.first<{ note: string | null }>('SELECT note FROM t WHERE id = ?', ['a'])).toEqual({ note: null });
    expect(await db.first('SELECT * FROM t WHERE id = ?', ['zzz'])).toBeNull();
  });

  it('commits an exclusive transaction and rolls back on throw', async () => {
    const db = openNodeDb();
    await db.exec('CREATE TABLE t (id TEXT PRIMARY KEY);');
    expect(await db.exclusive(async (tx) => { await tx.run('INSERT INTO t VALUES (?)', ['kept']); return 'ok'; })).toBe('ok');
    await expect(db.exclusive(async (tx) => {
      await tx.run('INSERT INTO t VALUES (?)', ['dropped']);
      throw new Error('boom');
    })).rejects.toThrow('boom');
    expect(await db.all('SELECT id FROM t')).toEqual([{ id: 'kept' }]);
  });

  it('serializes exclusive transactions', async () => {
    const db = openNodeDb();
    const order: string[] = [];
    const slow = db.exclusive(async () => {
      order.push('a:start');
      await new Promise((r) => setTimeout(r, 5));
      order.push('a:end');
    });
    const fast = db.exclusive(async () => { order.push('b'); });
    await Promise.all([slow, fast]);
    expect(order).toEqual(['a:start', 'a:end', 'b']);
  });
});

describe('migrate', () => {
  it('climbs the ladder once and records user_version', async () => {
    const db = openNodeDb();
    await migrate(db, LADDER);
    await migrate(db, LADDER);
    expect(await db.first('PRAGMA user_version')).toEqual({ user_version: 2 });
    await db.run('INSERT INTO items (id, title) VALUES (?, ?)', ['i', 'x']);
    expect(await db.first('SELECT done FROM items')).toEqual({ done: 0 });
  });

  it('resumes from the recorded version', async () => {
    const db = openNodeDb();
    await migrate(db, LADDER.slice(0, 1));
    await migrate(db, LADDER);
    expect(await db.first('PRAGMA user_version')).toEqual({ user_version: 2 });
  });

  it('applies a failing step atomically: no partial DDL, user_version unchanged, and a fixed re-run succeeds', async () => {
    const db = openNodeDb();
    const broken = [LADDER[0], 'CREATE TABLE tags (id TEXT PRIMARY KEY);\nALTER TABLE nope ADD COLUMN x INTEGER;'];
    await expect(migrate(db, broken)).rejects.toThrow(/nope/);
    expect(await db.first('PRAGMA user_version')).toEqual({ user_version: 1 });
    expect(await db.first("SELECT name FROM sqlite_master WHERE name = 'tags'")).toBeNull();

    await migrate(db, [LADDER[0], 'CREATE TABLE tags (id TEXT PRIMARY KEY);\nALTER TABLE items ADD COLUMN x INTEGER;']);
    expect(await db.first('PRAGMA user_version')).toEqual({ user_version: 2 });
    expect(await db.first("SELECT name FROM sqlite_master WHERE name = 'tags'")).toEqual({ name: 'tags' });
  });

  it('coalesces concurrent migrations on one handle', async () => {
    const db = openNodeDb();
    await Promise.all([migrate(db, LADDER), migrate(db, LADDER)]);
    expect(await db.first('PRAGMA user_version')).toEqual({ user_version: 2 });
  });
});
