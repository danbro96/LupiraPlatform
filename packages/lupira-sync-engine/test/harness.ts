import { openNodeDb } from '@danbro96/lupira-expo-sqlite/node';
import type { Db } from '@danbro96/lupira-expo-sqlite/types';
import { wins, type SectionGuard } from '@danbro96/lupira-sync-core/lww';
import { createSyncEngine, type SyncEngineOptions } from '../src/engine.ts';
import type { AggregateModule, ChangeEvent, DocState, FeedPage, IndexSpec, OpBase } from '../src/types.ts';

export type Note = { title: string; body: string };
export type NoteGuards = { title: SectionGuard; body: SectionGuard };
export type NoteOp = OpBase & { title?: string; group?: string };
export type NoteChange = { id: string; doc: Note; guards: NoteGuards };
type NoteModule = AggregateModule<Note, NoteGuards, NoteOp, NoteChange>;

export const T = (n: number): string => `2026-07-01T12:${String(n).padStart(2, '0')}:00.000Z`;
export const cmd = (n: number): string => `0198c0de-0000-7000-8000-${String(n).padStart(12, '0')}`;
const stamp = (n: number): SectionGuard => ({ ts: T(n), cmd: cmd(n) });

export class HttpError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`HTTP ${status}`);
    this.status = status;
  }
}

type OpFields = { aggregate?: string; group?: string };

const op = (n: number, kind: string, id: string, fields: OpFields & { title?: string }): NoteOp => ({
  commandId: cmd(n), occurredAt: T(n), aggregate: fields.aggregate ?? 'note', aggregateId: id, kind, ...fields,
});

export const create = (n: number, id: string, fields: OpFields = {}): NoteOp => op(n, 'create', id, { title: `Note ${id}`, ...fields });
export const retitle = (n: number, id: string, title: string, fields: OpFields = {}): NoteOp => op(n, 'retitle', id, { title, ...fields });
export const remove = (n: number, id: string, fields: OpFields = {}): NoteOp => op(n, 'delete', id, fields);

/** A section-LWW reducer shaped like the apps' own. */
export function reduceNote(state: DocState<Note, NoteGuards> | null, o: NoteOp): DocState<Note, NoteGuards> | null {
  const s = { ts: o.occurredAt, cmd: o.commandId };
  if (o.kind === 'create') return state ?? { doc: { title: o.title ?? '', body: '' }, guards: { title: s, body: s } };
  if (o.kind === 'delete') return null;
  if (!state || !wins(o.occurredAt, o.commandId, state.guards.title.ts, state.guards.title.cmd)) return state;
  return { doc: { ...state.doc, title: o.title ?? '' }, guards: { ...state.guards, title: s } };
}

export const serverNote = (id: string, title: string, titleAt = 0, body = '', bodyAt = 0): NoteChange => ({
  id, doc: { title, body }, guards: { title: stamp(titleAt), body: stamp(bodyAt) },
});

export const page = (cursor: string, over: Partial<FeedPage<NoteChange>> = {}): FeedPage<NoteChange> => ({
  cursor, hasMore: false, reset: false, changed: [], deleted: [], ...over,
});

export interface ScriptedFeed {
  calls: (string | null)[];
  /** Pages (or errors to throw) served in order; an empty script serves an empty page at the same cursor. */
  script: (FeedPage<NoteChange> | Error | (() => Promise<FeedPage<NoteChange>>))[];
  fetch(since: string | null): Promise<FeedPage<NoteChange>>;
  fromWire(change: NoteChange): { id: string; state: DocState<Note, NoteGuards> };
}

export function scriptedFeed(): ScriptedFeed {
  const feed: ScriptedFeed = {
    calls: [],
    script: [],
    async fetch(since) {
      feed.calls.push(since);
      const next = feed.script.shift() ?? page(since ?? '0');
      if (next instanceof Error) throw next;
      return typeof next === 'function' ? next() : next;
    },
    fromWire: (c) => ({ id: c.id, state: { doc: c.doc, guards: c.guards } }),
  };
  return feed;
}

export const titleIndex = (version = 1): IndexSpec<Note, NoteGuards> => ({
  version,
  tables: ['note_titles'],
  ddl: version === 1
    ? 'CREATE TABLE note_titles (id TEXT PRIMARY KEY, title TEXT NOT NULL);'
    : 'CREATE TABLE note_titles (id TEXT PRIMARY KEY, title TEXT NOT NULL, shout TEXT NOT NULL);',
  async write(tx, id, state) {
    await tx.run('DELETE FROM note_titles WHERE id = ?', [id]);
    if (!state) return;
    if (version === 1) await tx.run('INSERT INTO note_titles (id, title) VALUES (?, ?)', [id, state.doc.title]);
    else await tx.run('INSERT INTO note_titles (id, title, shout) VALUES (?, ?, ?)', [id, state.doc.title, state.doc.title.toUpperCase()]);
  },
});

export type Harness = ReturnType<typeof createHarness>;

/** Modules, in pull order: `note` (indexed), `list`, `task` (held by its list via `group`), `folder` (read-only). */
export function createHarness(db: Db = openNodeDb()) {
  const h = {
    db,
    clock: Date.parse('2026-07-01T13:00:00Z'),
    replayed: [] as string[],
    /** commandId → error its replay throws, until removed. */
    failures: new Map<string, Error>(),
    events: [] as ChangeEvent[],
    feeds: { note: scriptedFeed(), list: scriptedFeed(), task: scriptedFeed(), folder: scriptedFeed() },
    async replay(o: NoteOp): Promise<void> {
      const failure = h.failures.get(o.commandId);
      if (failure) throw failure;
      h.replayed.push(o.commandId);
    },
    modules(indexVersion = 1): AggregateModule[] {
      const writable = (aggregate: 'note' | 'list' | 'task', extra: Partial<NoteModule> = {}): NoteModule =>
        ({ aggregate, feed: h.feeds[aggregate], reduce: reduceNote, replay: (o) => h.replay(o), ...extra });
      return [
        writable('note', { index: titleIndex(indexVersion) }),
        writable('list'),
        writable('task', { holdKeyOf: (o) => o.group ?? o.aggregateId }),
        { aggregate: 'folder', feed: h.feeds.folder },
      ];
    },
    engine(over: Partial<SyncEngineOptions> = {}) {
      return createSyncEngine({
        openDb: async () => h.db,
        modules: h.modules(),
        cacheVersion: 1,
        onChange: (e) => h.events.push(e),
        now: () => h.clock,
        ...over,
      });
    },
    async outbox() {
      return db.all<{ command_id: string; status: string; attempts: number; last_status: number | null }>(
        'SELECT command_id, status, attempts, last_status FROM outbox ORDER BY seq');
    },
    async titles() {
      return db.all<{ id: string; title: string }>('SELECT id, title FROM note_titles ORDER BY id');
    },
  };
  return h;
}
