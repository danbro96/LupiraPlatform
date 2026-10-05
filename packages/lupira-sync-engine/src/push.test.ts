import { PARK_AFTER_ATTEMPTS } from '@danbro96/lupira-sync-core/backoff';
import { beforeEach, describe, expect, it } from 'vitest';
import { cmd, create, createHarness, HttpError, remove, retitle, type Harness } from '../test/harness.ts';

const HOUR = 60 * 60_000;

let h: Harness;
let engine: ReturnType<Harness['engine']>;

beforeEach(() => {
  h = createHarness();
  engine = h.engine();
});

describe('enqueue', () => {
  it('writes the local doc, its index row and the queued op together', async () => {
    h.failures.set(cmd(1), new HttpError(0));
    await engine.enqueue(create(1, 'a'));
    await engine.push();

    expect(await engine.doc('note', 'a')).toMatchObject({ doc: { title: 'Note a' } });
    expect(await h.titles()).toEqual([{ id: 'a', title: 'Note a' }]);
    expect(await h.outbox()).toMatchObject([{ command_id: cmd(1), status: 'pending' }]);
    expect(engine.status.getSnapshot().pending).toBe(1);
  });

  it('rolls a failing batch back whole: no doc, no op', async () => {
    await expect(engine.enqueue([create(1, 'a'), create(1, 'b')])).rejects.toThrow(/UNIQUE/);

    expect(await engine.doc('note', 'a')).toBeNull();
    expect(await h.outbox()).toEqual([]);
  });

  it('refuses ops for a read-only aggregate', async () => {
    await expect(engine.enqueue(create(1, 'f', { aggregate: 'folder' }))).rejects.toThrow(/read-only/);
  });
});

describe('drain', () => {
  it('replays in order, deletes acked ops and keeps their effect in the server base', async () => {
    await engine.enqueue([create(1, 'a'), retitle(2, 'a', 'v2')]);
    await engine.push();

    expect(h.replayed).toEqual([cmd(1), cmd(2)]);
    expect(await h.outbox()).toEqual([]);
    expect(await engine.doc('note', 'a')).toMatchObject({ doc: { title: 'v2' } });
    const row = await h.db.first<{ server: string }>("SELECT server FROM docs WHERE id = 'a'");
    expect(JSON.parse(row!.server)).toMatchObject({ doc: { title: 'v2' } });
  });

  it('backs off a transient failure: not retried until due, retried after', async () => {
    h.failures.set(cmd(1), new HttpError(503));
    await engine.enqueue(create(1, 'a'));
    await engine.push();
    expect(await h.outbox()).toMatchObject([{ status: 'pending', attempts: 1, last_status: 503 }]);

    h.failures.clear();
    await engine.push();
    expect(h.replayed).toEqual([]);

    h.clock += HOUR;
    await engine.push();
    expect(h.replayed).toEqual([cmd(1)]);
    expect(engine.status.getSnapshot()).toMatchObject({ pending: 0, serverReachable: true });
  });

  it('parks after enough consecutive transient failures', async () => {
    h.failures.set(cmd(1), new HttpError(503));
    await engine.enqueue(create(1, 'a'));
    for (let i = 0; i < PARK_AFTER_ATTEMPTS + 2; i++) {
      await engine.push();
      h.clock += HOUR;
    }
    expect(await h.outbox()).toMatchObject([{ status: 'parked', attempts: PARK_AFTER_ATTEMPTS }]);
    expect(engine.status.getSnapshot()).toMatchObject({ pending: 0, parked: 1 });
  });

  it('parks a semantic 4xx at once and holds later ops of the same aggregate only', async () => {
    h.failures.set(cmd(1), new HttpError(404));
    await engine.enqueue([retitle(1, 'dead', 'a'), retitle(2, 'dead', 'b'), create(3, 'live')]);
    await engine.push();

    expect(h.replayed).toEqual([cmd(3)]);
    expect(await h.outbox()).toMatchObject([{ status: 'parked' }, { status: 'pending' }]);
    expect(await engine.parked()).toMatchObject([{ op: { commandId: cmd(1) }, lastStatus: 404, lastError: 'rejected (404)' }]);
  });

  it('holds a backed-off op\'s aggregate while another proceeds, then replays both in order', async () => {
    h.failures.set(cmd(1), new HttpError(503));
    await engine.enqueue([create(1, 'a'), retitle(2, 'a', 'v2'), create(3, 'b')]);
    await engine.push();
    expect(h.replayed).toEqual([cmd(3)]);

    h.failures.clear();
    h.clock += HOUR;
    await engine.push();
    expect(h.replayed).toEqual([cmd(3), cmd(1), cmd(2)]);
  });

  it('holds by hold key across aggregates: a parked list op holds its tasks, not other lists\' tasks', async () => {
    h.failures.set(cmd(1), new HttpError(422));
    await engine.enqueue([
      create(1, 'L1', { aggregate: 'list' }),
      create(2, 't1', { aggregate: 'task', group: 'L1' }),
      create(3, 't2', { aggregate: 'task', group: 'L2' }),
    ]);
    await engine.push();

    expect(h.replayed).toEqual([cmd(3)]);
    expect(await h.outbox()).toMatchObject([{ command_id: cmd(1), status: 'parked' }, { command_id: cmd(2), status: 'pending' }]);
  });

  it('pauses on 401 without touching the op, ignores drains until the next sync', async () => {
    h.failures.set(cmd(1), new HttpError(401));
    await engine.enqueue(create(1, 'a'));
    await engine.push();
    expect(await h.outbox()).toMatchObject([{ status: 'pending', attempts: 0 }]);
    expect(engine.status.getSnapshot().lastError).toBe('signed out');

    h.failures.clear();
    await engine.enqueue(create(2, 'b'));
    await engine.push();
    expect(h.replayed).toEqual([]);

    await engine.sync();
    expect(h.replayed).toEqual([cmd(1), cmd(2)]);
  });

  it('drains an op enqueued while a replay is in flight in the same push', async () => {
    let release!: () => void;
    const first = new Promise<void>((r) => {
      release = r;
    });
    const replay = h.replay;
    h.replay = async (o) => {
      if (o.commandId === cmd(1)) await first;
      return replay(o);
    };
    await engine.enqueue(create(1, 'a'));
    const pushing = engine.push();
    await engine.enqueue(create(2, 'b'));
    release();
    await pushing;

    expect(h.replayed).toEqual([cmd(1), cmd(2)]);
    expect(await h.outbox()).toEqual([]);
  });
});

describe('retry and discard', () => {
  it('retry requeues a parked op; the op it held follows', async () => {
    h.failures.set(cmd(1), new HttpError(422));
    await engine.enqueue([create(1, 'a'), retitle(2, 'a', 'v2')]);
    await engine.push();
    expect(h.replayed).toEqual([]);

    h.failures.clear();
    await engine.retry(cmd(1));
    await engine.push();
    expect(h.replayed).toEqual([cmd(1), cmd(2)]);
  });

  it('retry clears a backoff so the op replays at once', async () => {
    h.failures.set(cmd(1), new HttpError(503));
    await engine.enqueue(create(1, 'a'));
    await engine.push();

    h.failures.clear();
    await engine.retry(cmd(1));
    await engine.push();
    expect(h.replayed).toEqual([cmd(1)]);
  });

  it.each([['parked', 422], ['backed-off', 503]])('discarding a %s op rolls its effect back and releases the ops behind it', async (_label, status) => {
    h.failures.set(cmd(2), new HttpError(status));
    await engine.enqueue(create(1, 'a'));
    await engine.push();
    await engine.enqueue([retitle(2, 'a', 'doomed'), retitle(3, 'a', 'kept')]);
    await engine.push();
    expect(h.replayed).toEqual([cmd(1)]);

    await engine.discard(cmd(2));
    await engine.push();

    expect(h.replayed).toEqual([cmd(1), cmd(3)]);
    expect(await engine.doc('note', 'a')).toMatchObject({ doc: { title: 'kept' } });
  });

  it('discarding the only op of a local create removes the doc and its index row', async () => {
    h.failures.set(cmd(1), new HttpError(422));
    await engine.enqueue(create(1, 'a'));
    await engine.push();

    await engine.discard(cmd(1));

    expect(await engine.doc('note', 'a')).toBeNull();
    expect(await h.titles()).toEqual([]);
    expect(await h.db.all('SELECT * FROM docs')).toEqual([]);
  });

  it('a held op stays queued until due and is cancelled by discard (undo)', async () => {
    await engine.enqueue(create(1, 'a'));
    await engine.push();
    await engine.enqueue(remove(2, 'a'), { holdMs: 5_000 });
    await engine.push();
    expect(await engine.doc('note', 'a')).toBeNull();
    expect(h.replayed).toEqual([cmd(1)]);

    await engine.discard(cmd(2));
    h.clock += 10_000;
    await engine.push();

    expect(h.replayed).toEqual([cmd(1)]);
    expect(await engine.doc('note', 'a')).toMatchObject({ doc: { title: 'Note a' } });
  });

  it('a held op replays once its hold elapses', async () => {
    await engine.enqueue(create(1, 'a'), { holdMs: 5_000 });
    await engine.push();
    expect(h.replayed).toEqual([]);

    h.clock += 5_000;
    await engine.push();
    expect(h.replayed).toEqual([cmd(1)]);
  });
});
