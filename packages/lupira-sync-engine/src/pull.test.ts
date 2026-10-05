import { beforeEach, describe, expect, it } from 'vitest';
import { cmd, create, createHarness, HttpError, page, retitle, serverNote, type Harness } from '../test/harness.ts';

let h: Harness;
let engine: ReturnType<Harness['engine']>;

beforeEach(() => {
  h = createHarness();
  engine = h.engine();
});

const titles = async (aggregate = 'note') => (await engine.docs<{ title: string }, unknown>(aggregate)).map((d) => d.state.doc.title);

async function enqueueOffline(...ops: Parameters<typeof engine.enqueue>[0][]): Promise<void> {
  for (const o of ops.flat()) h.failures.set(o.commandId, new HttpError(0));
  await engine.enqueue(ops.flat());
  await engine.push();
}

describe('paging and cursors', () => {
  it('pages while hasMore, stores the cursor per page and resumes from it next sync', async () => {
    h.feeds.note.script.push(
      page('10', { hasMore: true, changed: [serverNote('a', 'A')] }),
      page('20', { changed: [serverNote('b', 'B')] }),
    );
    await engine.sync();
    expect(h.feeds.note.calls).toEqual([null, '10']);
    expect(await titles()).toEqual(['A', 'B']);
    expect(await h.titles()).toEqual([{ id: 'a', title: 'A' }, { id: 'b', title: 'B' }]);

    await engine.sync();
    expect(h.feeds.note.calls).toEqual([null, '10', '20']);
  });

  it('pulls modules in config order', async () => {
    const order: string[] = [];
    for (const [name, feed] of Object.entries(h.feeds)) feed.script.push(async () => (order.push(name), page('1')));
    await engine.sync();
    expect(order).toEqual(['note', 'list', 'task', 'folder']);
  });

  it('applies a delta and leaves unmentioned docs alone', async () => {
    h.feeds.note.script.push(page('1', { changed: [serverNote('a', 'A'), serverNote('b', 'B')] }));
    await engine.sync();
    h.feeds.note.script.push(page('2', { changed: [serverNote('b', 'B2', 1)] }));
    await engine.sync();
    expect(await titles()).toEqual(['A', 'B2']);
  });
});

describe('rebase through queued ops', () => {
  it('keeps a pending edit over an older server section and takes the fresher other section', async () => {
    await enqueueOffline([create(1, 'a'), retitle(30, 'a', 'Local edit')]);

    h.feeds.note.script.push(page('5', { changed: [serverNote('a', 'Server', 10, 'web', 40)] }));
    await engine.sync();

    expect(await engine.doc('note', 'a')).toMatchObject({ doc: { title: 'Local edit', body: 'web' } });
    expect(await h.titles()).toEqual([{ id: 'a', title: 'Local edit' }]);
  });

  it('lets a newer server section win over a stale queued edit', async () => {
    await enqueueOffline(retitle(10, 'a', 'Stale local'));

    h.feeds.note.script.push(page('5', { changed: [serverNote('a', 'Newer server', 20)] }));
    await engine.sync();

    expect(await titles()).toEqual(['Newer server']);
  });

  it('keeps a parked op in the fold until it is discarded', async () => {
    h.failures.set(cmd(30), new HttpError(422));
    await engine.enqueue(retitle(30, 'a', 'Parked edit'));
    await engine.push();
    h.feeds.note.script.push(page('5', { changed: [serverNote('a', 'Server', 10)] }));
    await engine.sync();
    expect(await titles()).toEqual(['Parked edit']);

    await engine.discard(cmd(30));
    expect(await titles()).toEqual(['Server']);
  });
});

describe('tombstones', () => {
  it('removes the doc and its index row', async () => {
    h.feeds.note.script.push(page('5', { changed: [serverNote('doomed', 'D')] }), page('9', { deleted: ['doomed'] }));
    await engine.sync();
    await engine.sync();

    expect(await engine.doc('note', 'doomed')).toBeNull();
    expect(await h.titles()).toEqual([]);
    expect(await h.db.all('SELECT * FROM docs')).toEqual([]);
  });

  it('changes nothing for ids never held', async () => {
    await engine.sync();
    h.events.length = 0;
    h.feeds.note.script.push(page('2', { deleted: ['foreign-1', 'foreign-2'] }));
    await engine.sync();
    expect(h.events).toEqual([]);
  });
});

describe('full sync', () => {
  it('prunes unmentioned docs but keeps local creates and docs with queued ops', async () => {
    h.feeds.note.script.push(page('5', { changed: [serverNote('stale', 'S'), serverNote('edited', 'E')] }));
    await engine.sync();
    await enqueueOffline(create(1, 'local-only'), retitle(2, 'edited', 'E2'));
    await h.db.run('UPDATE cursors SET cursor = NULL');

    h.feeds.note.script.push(page('50', { changed: [serverNote('kept', 'K')] }));
    await engine.sync();

    expect(await titles()).toEqual(['E2', 'K', 'Note local-only']);
    expect((await h.titles()).map((r) => r.id)).toEqual(['edited', 'kept', 'local-only']);
  });

  it('turns a delta into a full sync on a reset page', async () => {
    h.feeds.note.script.push(page('5', { changed: [serverNote('revoked', 'R')] }));
    await engine.sync();
    h.feeds.note.script.push(page('9', { reset: true, changed: [serverNote('shared', 'S')] }));
    await engine.sync();
    expect(await titles()).toEqual(['S']);
  });

  it('resumes an interrupted full sync from its cursor in the same generation', async () => {
    h.feeds.note.script.push(page('1', { changed: [serverNote('old', 'O')] }));
    await engine.sync();

    h.feeds.note.script.push(
      page('10', { reset: true, hasMore: true, changed: [serverNote('a', 'A')] }),
      new HttpError(0),
    );
    await engine.sync();
    expect(engine.status.getSnapshot()).toMatchObject({ lastError: 'HTTP 0', serverReachable: false });
    expect(await titles()).toEqual(['A', 'O']);

    h.feeds.note.script.push(page('20', { changed: [serverNote('b', 'B')] }));
    await engine.sync();
    expect(h.feeds.note.calls.slice(-2)).toEqual(['10', '10']);
    expect(await titles()).toEqual(['A', 'B']);

    await engine.sync();
    expect(h.feeds.note.calls.at(-1)).toBe('20');
    expect(await titles()).toEqual(['A', 'B']);
  });

  it('a snapshot feed replaces a read-only aggregate on every call', async () => {
    const snapshot = (...ids: string[]) => page('s', { reset: true, changed: ids.map((id) => serverNote(id, id)) });
    h.feeds.folder.script.push(snapshot('f1', 'f2'));
    await engine.sync();
    expect(await titles('folder')).toEqual(['f1', 'f2']);

    h.events.length = 0;
    h.feeds.folder.script.push(snapshot('f1', 'f2'));
    await engine.sync();
    expect(h.events).toEqual([]);

    h.feeds.folder.script.push(snapshot('f2', 'f3'));
    await engine.sync();
    expect(await titles('folder')).toEqual(['f2', 'f3']);
    expect(h.events).toEqual([{ aggregate: 'folder', ids: ['f3', 'f1'], origin: 'pull' }]);
  });
});

describe('push and pull share the wire', () => {
  it('holds a replay until an in-flight page has been applied', async () => {
    let release!: () => void;
    h.feeds.note.script.push(() => new Promise((r) => {
      release = () => r(page('1', { changed: [serverNote('a', 'A')] }));
    }));
    const syncing = engine.sync();
    await new Promise((r) => setTimeout(r, 5));

    await engine.enqueue(retitle(30, 'a', 'Mine'));
    await new Promise((r) => setTimeout(r, 5));
    expect(h.replayed).toEqual([]);

    release();
    await syncing;
    await engine.push();
    expect(h.replayed).toEqual([cmd(30)]);
    expect(await titles()).toEqual(['Mine']);
  });
});

describe('failures', () => {
  it('never rejects: the error and reachability land in status', async () => {
    h.feeds.note.script.push(new HttpError(500));
    await expect(engine.sync()).resolves.toBeUndefined();
    expect(engine.status.getSnapshot()).toMatchObject({ phase: 'idle', lastError: 'HTTP 500', serverReachable: false, lastSyncAt: null });

    await engine.sync();
    expect(engine.status.getSnapshot()).toMatchObject({ lastError: null, serverReachable: true, lastSyncAt: h.clock });
  });
});
