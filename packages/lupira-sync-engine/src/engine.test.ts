import { describe, expect, it } from 'vitest';
import { cmd, create, createHarness, HttpError, page, retitle, serverNote } from '../test/harness.ts';

describe('opening', () => {
  it('survives concurrent first use on a virgin database', async () => {
    const h = createHarness();
    const engine = h.engine();
    await Promise.all([engine.sync(), engine.enqueue(create(1, 'a')), engine.doc('note', 'a'), h.engine().doc('note', 'a')]);
    await engine.push();

    expect(await h.db.first('PRAGMA user_version')).toEqual({ user_version: 1 });
    expect(h.replayed).toEqual([cmd(1)]);
  });

  it('rebuilds a module index when its version changes', async () => {
    const h = createHarness();
    h.feeds.note.script.push(page('1', { changed: [serverNote('a', 'Alpha')] }));
    await h.engine().sync();
    h.events.length = 0;

    const upgraded = h.engine({ modules: h.modules(2) });
    await upgraded.doc('note', 'a');

    expect(await h.db.all('SELECT id, shout FROM note_titles')).toEqual([{ id: 'a', shout: 'ALPHA' }]);
    expect(h.events).toEqual([{ aggregate: 'note', ids: ['a'], origin: 'local' }]);

    h.events.length = 0;
    await h.engine({ modules: h.modules(2) }).doc('note', 'a');
    expect(h.events).toEqual([]);
  });

  it('drops pulled data on a cacheVersion bump but keeps queued ops and their effect', async () => {
    const h = createHarness();
    h.feeds.note.script.push(page('1', { changed: [serverNote('a', 'A'), serverNote('b', 'B')] }));
    await h.engine().sync();
    h.failures.set(cmd(1), new HttpError(0));
    const engine = h.engine();
    await engine.enqueue(retitle(1, 'a', 'Mine'));
    await engine.push();

    const bumped = h.engine({ cacheVersion: 2 });
    expect(await bumped.docs('note')).toEqual([]);
    expect(await h.titles()).toEqual([]);
    expect(await h.outbox()).toMatchObject([{ command_id: cmd(1) }]);

    h.feeds.note.script.push(page('7', { changed: [serverNote('a', 'A'), serverNote('b', 'B')] }));
    await bumped.sync();
    expect(h.feeds.note.calls.at(-1)).toBeNull();
    expect((await bumped.docs<{ title: string }, unknown>('note')).map((d) => d.state.doc.title)).toEqual(['Mine', 'B']);
  });
});

describe('reindex', () => {
  it('rewrites every visible doc into the index', async () => {
    const h = createHarness();
    const engine = h.engine();
    h.feeds.note.script.push(page('1', { changed: [serverNote('a', 'A'), serverNote('b', 'B')] }));
    await engine.sync();
    await h.db.run('DELETE FROM note_titles');

    await engine.reindex('note');

    expect(await h.titles()).toEqual([{ id: 'a', title: 'A' }, { id: 'b', title: 'B' }]);
  });
});

describe('wipe', () => {
  it('clears docs, outbox, cursors and index rows', async () => {
    const h = createHarness();
    const engine = h.engine();
    h.feeds.note.script.push(page('1', { changed: [serverNote('a', 'A')] }));
    await engine.sync();
    h.failures.set(cmd(1), new HttpError(0));
    await engine.enqueue(create(1, 'b'));
    await engine.push();

    await engine.wipe();

    expect(await engine.docs('note')).toEqual([]);
    expect(await h.outbox()).toEqual([]);
    expect(await h.titles()).toEqual([]);
    expect(await h.db.all('SELECT * FROM cursors')).toEqual([]);
    expect(engine.status.getSnapshot()).toMatchObject({ pending: 0, parked: 0 });
  });
});

describe('onChange', () => {
  it('fires after commit, once per aggregate per transaction, tagged with its origin', async () => {
    const h = createHarness();
    const engine = h.engine();
    h.failures.set(cmd(1), new HttpError(0));
    h.failures.set(cmd(2), new HttpError(0));
    await engine.enqueue([create(1, 'a'), create(2, 'L1', { aggregate: 'list' }), retitle(3, 'a', 'again')]);
    expect(h.events).toEqual([
      { aggregate: 'note', ids: ['a'], origin: 'local' },
      { aggregate: 'list', ids: ['L1'], origin: 'local' },
    ]);

    h.events.length = 0;
    h.feeds.note.script.push(page('1', { changed: [serverNote('x', 'X'), serverNote('y', 'Y')] }));
    await engine.sync();
    expect(h.events).toEqual([{ aggregate: 'note', ids: ['x', 'y'], origin: 'pull' }]);
  });

  it('stays quiet when a replay is acknowledged, since the visible doc is unchanged', async () => {
    const h = createHarness();
    const engine = h.engine();
    await engine.enqueue(create(1, 'a'));
    h.events.length = 0;
    await engine.push();
    expect(h.replayed).toEqual([cmd(1)]);
    expect(h.events).toEqual([]);
  });
});

describe('status', () => {
  it('notifies subscribers and returns to idle with lastSyncAt after a sync', async () => {
    const h = createHarness();
    const engine = h.engine();
    const phases: string[] = [];
    engine.status.subscribe(() => phases.push(engine.status.getSnapshot().phase));

    await engine.sync();

    expect(phases).toContain('push');
    expect(phases).toContain('pull');
    expect(engine.status.getSnapshot()).toMatchObject({ phase: 'idle', progress: null, lastSyncAt: h.clock, lastError: null });
  });

  it('runs the hooks around push and pull', async () => {
    const h = createHarness();
    const order: string[] = [];
    const engine = h.engine({
      hooks: {
        beforePush: async () => {
          order.push('beforePush');
        },
        afterPull: async () => {
          order.push('afterPull');
        },
      },
    });
    h.feeds.note.script.push(async () => (order.push('pull'), page('1')));
    const replay = h.replay;
    h.replay = async (o) => (order.push('push'), replay(o));
    await engine.enqueue(create(1, 'a'), { holdMs: 1 });
    h.clock += 1;

    await engine.sync();

    expect(order).toEqual(['beforePush', 'push', 'pull', 'afterPull']);
  });
});
