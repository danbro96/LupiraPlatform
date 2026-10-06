import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { applySchema } from './applySchema.ts';
import { openNodeDb } from './nodeDb.ts';
import type { Db } from './types.ts';

describe('applySchema', () => {
  let dir: string;
  beforeEach(() => {
    dir = mkdtempSync(join(tmpdir(), 'lupira-expo-sqlite-'));
  });
  afterEach(() => rmSync(dir, { recursive: true, force: true }));

  const readerMidIteration = () => {
    const file = join(dir, 'app.db');
    const reader = new DatabaseSync(file);
    reader.exec('PRAGMA journal_mode = WAL; CREATE TABLE seed (n INTEGER); INSERT INTO seed VALUES (1), (2);');
    reader.prepare('SELECT n FROM seed').iterate().next();
    return { file, reader };
  };

  it('a reader holding a snapshot misses a table another connection creates', () => {
    const { file, reader } = readerMidIteration();
    new DatabaseSync(file).exec('BEGIN IMMEDIATE; CREATE TABLE meta (k TEXT); COMMIT;');
    expect(() => reader.prepare('SELECT k FROM meta').all()).toThrow(/no such table: meta/);
  });

  it('accepts DDL without a trailing semicolon', async () => {
    const { reader } = readerMidIteration();
    const db = { exec: async (sql: string) => reader.exec(sql) } as unknown as Db;
    await applySchema(db, 'CREATE TABLE meta (k TEXT)');
    expect(reader.prepare('SELECT count(*) AS n FROM meta').get()).toEqual({ n: 0 });
  });

  it('makes the table visible to the reader when run on its connection', async () => {
    const { reader } = readerMidIteration();
    const db = { exec: async (sql: string) => reader.exec(sql) } as unknown as Db;
    await applySchema(db, 'CREATE TABLE meta (k TEXT);');
    expect(reader.prepare('SELECT k FROM meta').all()).toEqual([]);
  });

  it('rolls back a failing script and leaves the connection usable', async () => {
    const db = openNodeDb();
    await expect(applySchema(db, 'CREATE TABLE a (id TEXT);\nALTER TABLE nope ADD COLUMN x INTEGER;')).rejects.toThrow(/nope/);
    expect(await db.first("SELECT name FROM sqlite_master WHERE name = 'a'")).toBeNull();
    await applySchema(db, 'CREATE TABLE a (id TEXT);');
    expect(await db.first("SELECT name FROM sqlite_master WHERE name = 'a'")).toEqual({ name: 'a' });
  });
});
