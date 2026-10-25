import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, NETWORK_ERROR } from './apiError.ts';
import { authPort, setAuthPort, type AuthPort } from './authPort.ts';
import { createBearerMutator, type BearerMutatorOptions } from './mutator.ts';

type FetchCall = { url: string; headers: Headers };
let calls: FetchCall[];
let responses: Array<() => Response>;

const session = (current: string | null, rotated: string | null = current) =>
  vi.fn(async (force?: boolean, _sentToken?: string) => (force ? rotated : current));

const forcedRefreshes = (refresh: ReturnType<typeof session>) => refresh.mock.calls.filter(([force]) => force).length;

function stubPort(overrides: Partial<AuthPort> = {}): AuthPort {
  const port: AuthPort = {
    getApiUrl: () => 'https://bff.test',
    getToken: () => 'tok-1',
    refresh: session('tok-1'),
    onSignIn: () => () => {},
    onSignOut: () => () => {},
    ...overrides,
  };
  setAuthPort(port);
  return port;
}

const mutator = (options: Partial<BearerMutatorOptions> = {}) => createBearerMutator({ auth: authPort, timeoutMs: 10_000, ...options });

beforeEach(() => {
  calls = [];
  responses = [];
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit) => {
    calls.push({ url, headers: new Headers(init.headers) });
    const next = responses.shift();
    if (!next) throw new Error('unexpected fetch');
    return next();
  }));
  vi.useFakeTimers();
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

const json = (status: number, body: unknown = {}, headers?: Record<string, string>) => () =>
  new Response(JSON.stringify(body), { status, headers });
const text = (status: number, body = '') => () => new Response(body, { status });

/** Runs a request while draining the retry-delay timers fake time creates. The synchronous no-op catch marks
 *  a rejection as handled before the timer drain, so expect(...).rejects doesn't race an unhandled-rejection. */
async function run<T>(promise: Promise<T>): Promise<T> {
  promise.catch(() => undefined);
  await vi.runAllTimersAsync();
  return promise;
}

describe('createBearerMutator', () => {
  it('prefixes the backend origin and injects the bearer', async () => {
    stubPort();
    responses.push(json(200, { ok: true }));
    const r = await run(mutator()<{ ok: boolean }>('/api/me'));

    expect(calls[0].url).toBe('https://bff.test/api/me');
    expect(calls[0].headers.get('Authorization')).toBe('Bearer tok-1');
    expect(calls[0].headers.get('Accept')).toBe('application/json');
    expect(calls[0].headers.get('Cache-Control')).toBe('no-store');
    expect(r).toEqual({ ok: true });
  });

  it('takes an explicit base URL and a header decorator', async () => {
    stubPort({ refresh: session(null) });
    responses.push(json(200));
    await run(mutator({ baseUrl: () => 'https://other.test/', decorate: (h) => h.set('X-Dev-User', 'dev') })('/me'));
    expect(calls[0].url).toBe('https://other.test/me');
    expect(calls[0].headers.get('X-Dev-User')).toBe('dev');
  });

  it('sends no Authorization header in dev auto-auth mode', async () => {
    stubPort({ refresh: session(null) });
    responses.push(json(200));
    await run(mutator()('/api/me'));
    expect(calls[0].headers.has('Authorization')).toBe(false);
  });

  it('sets a JSON content type for a body, but leaves FormData alone', async () => {
    stubPort();
    responses.push(json(200), json(200));
    await run(mutator()('/api/items', { method: 'POST', body: '{}' }));
    await run(mutator()('/api/photos', { method: 'POST', body: new FormData() }));
    expect(calls[0].headers.get('Content-Type')).toBe('application/json');
    expect(calls[1].headers.get('Content-Type')).not.toBe('application/json');
  });

  it('refreshes a token near expiry before the first request and sends the refreshed one', async () => {
    const order: string[] = [];
    const refresh = vi.fn(async (force?: boolean) => {
      await new Promise((resolve) => setTimeout(resolve, 100));
      order.push(`refresh(${force})`);
      return force ? 'tok-3' : 'tok-2';
    });
    stubPort({ getToken: () => 'tok-expired', refresh });
    responses.push(() => {
      order.push('fetch');
      return new Response('{}', { status: 200 });
    });

    await run(mutator()('/api/me'));
    expect(order).toEqual(['refresh(false)', 'fetch']);
    expect(calls).toHaveLength(1);
    expect(calls[0].headers.get('Authorization')).toBe('Bearer tok-2');
  });

  it('sends a fresh token as is: one request, no forced refresh', async () => {
    const refresh = session('tok-1', 'tok-2');
    stubPort({ refresh });
    responses.push(json(200));

    await run(mutator()('/api/me'));
    expect(calls).toHaveLength(1);
    expect(refresh).toHaveBeenCalledExactlyOnceWith(false);
    expect(calls[0].headers.get('Authorization')).toBe('Bearer tok-1');
  });

  it('retries a transient 503 on reads and then succeeds', async () => {
    stubPort();
    responses.push(json(503), json(200, { ok: true }));
    const r = await run(mutator()<{ ok: boolean }>('/api/me'));
    expect(calls).toHaveLength(2);
    expect(r).toEqual({ ok: true });
  });

  it('does not retry when retry is off', async () => {
    stubPort();
    responses.push(json(503));
    await expect(run(mutator({ retry: false })('/api/me'))).rejects.toMatchObject({ status: 503 });
    expect(calls).toHaveLength(1);
  });

  it.each(['POST', 'PUT', 'PATCH', 'DELETE'])('never retries a %s on a transient status, even with an Idempotency-Key', async (method) => {
    stubPort();
    responses.push(json(503));
    const init = { method, body: '{}', headers: { 'Idempotency-Key': '0198c0de-0000-7000-8000-000000000000' } };
    await expect(run(mutator()('/api/items/x', init))).rejects.toMatchObject({ status: 503 });
    expect(calls).toHaveLength(1);
  });

  it('never retries a write on a transport failure', async () => {
    stubPort();
    const failing = vi.fn(async () => {
      throw new TypeError('network down');
    });
    vi.stubGlobal('fetch', failing);
    await expect(run(mutator()('/api/items', { method: 'POST', body: '{}' }))).rejects.toMatchObject({ status: 0 });
    expect(failing).toHaveBeenCalledTimes(1);
  });

  it('forces one refresh on 401 and replays with the rotated token', async () => {
    const refresh = session('tok-1', 'tok-2');
    stubPort({ refresh });
    responses.push(json(401), json(200));

    const r = await run(mutator()<{ ok: boolean }>('/api/me'));
    expect(refresh).toHaveBeenCalledWith(true, 'tok-1');
    expect(calls[1].headers.get('Authorization')).toBe('Bearer tok-2');
    expect(r).toEqual({});
  });

  it.each(['POST', 'PUT', 'DELETE'])('forces one refresh on 401 and replays a %s with the rotated token', async (method) => {
    const refresh = session('tok-1', 'tok-2');
    stubPort({ refresh });
    responses.push(json(401), json(200, { id: 'x' }));

    const r = await run(mutator()<{ id: string }>('/api/items/x', { method, body: '{}' }));
    expect(forcedRefreshes(refresh)).toBe(1);
    expect(calls).toHaveLength(2);
    expect(calls[1].headers.get('Authorization')).toBe('Bearer tok-2');
    expect(r).toEqual({ id: 'x' });
  });

  it('does not replay a write when the forced refresh returns the same token', async () => {
    const refresh = session('tok-1');
    stubPort({ refresh });
    responses.push(json(401));

    await expect(run(mutator()('/api/items', { method: 'POST', body: '{}' }))).rejects.toMatchObject({ status: 401 });
    expect(refresh).toHaveBeenCalledWith(true, 'tok-1');
    expect(calls).toHaveLength(1);
  });

  it('gives up when the forced refresh returns the same token', async () => {
    const refresh = session('tok-1');
    stubPort({ refresh });
    responses.push(json(401));

    await expect(run(mutator()('/api/me'))).rejects.toMatchObject({ status: 401 });
    expect(forcedRefreshes(refresh)).toBe(1);
    expect(calls).toHaveLength(1);
  });

  it('gives up when the forced refresh cleared the session', async () => {
    const refresh = session('tok-1', null);
    stubPort({ refresh });
    responses.push(json(401));

    await expect(run(mutator()('/api/me'))).rejects.toMatchObject({ status: 401 });
    expect(calls).toHaveLength(1);
  });

  it('re-auths at most once: a second 401 after a successful refresh throws', async () => {
    const refresh = session('tok-1', 'tok-2');
    stubPort({ refresh });
    responses.push(json(401), json(401));

    await expect(run(mutator()('/api/me'))).rejects.toMatchObject({ status: 401 });
    expect(forcedRefreshes(refresh)).toBe(1);
    expect(calls).toHaveLength(2);
  });

  it('does not re-auth when no token was sent, nor treat a 403 as an auth failure', async () => {
    const anonymous = session(null, 'tok-2');
    stubPort({ refresh: anonymous });
    responses.push(json(401), json(403));

    await expect(run(mutator()('/api/items', { method: 'POST', body: '{}' }))).rejects.toMatchObject({ status: 401 });
    const signedIn = session('tok-1', 'tok-2');
    stubPort({ refresh: signedIn });
    await expect(run(mutator()('/api/me'))).rejects.toMatchObject({ status: 403 });
    expect(forcedRefreshes(anonymous) + forcedRefreshes(signedIn)).toBe(0);
  });

  it('surfaces a problem document’s detail, title and traceId, else the status', async () => {
    stubPort();
    responses.push(json(409, { title: 'Conflict', detail: 'Already linked', traceId: '00-abc-01' }), text(404));
    await expect(run(mutator()('/api/a'))).rejects.toMatchObject({
      status: 409,
      message: 'Already linked',
      title: 'Conflict',
      traceId: '00-abc-01',
    });
    await expect(run(mutator()('/api/b'))).rejects.toMatchObject({ status: 404, message: 'HTTP 404', title: undefined });
  });

  it('maps transport failure to ApiError(0) after the retry budget', async () => {
    stubPort();
    const failing = vi.fn(async () => {
      throw new TypeError('network down');
    });
    vi.stubGlobal('fetch', failing);

    const guarded = mutator()('/api/me').catch((e: unknown) => e);
    await vi.runAllTimersAsync();
    const err = await guarded;
    expect(err).toBeInstanceOf(ApiError);
    expect(err).toMatchObject({ status: 0, message: NETWORK_ERROR });
    expect(failing.mock.calls.length).toBe(3);
  });

  it('aborts an attempt at the timeout', async () => {
    stubPort();
    vi.stubGlobal('fetch', vi.fn((_url: string, init: RequestInit) => new Promise((_resolve, reject) => {
      init.signal?.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError')));
    })));
    const guarded = mutator({ timeoutMs: 50, retry: false })('/api/me').catch((e: unknown) => e);
    await vi.advanceTimersByTimeAsync(50);
    expect(await guarded).toMatchObject({ status: 0 });
  });

  it('returns the parsed body: undefined for 204, plain text as text, JSON as an object', async () => {
    stubPort();
    responses.push(() => new Response(null, { status: 204 }), text(200, 'pong'), json(201, { id: 'x' }));
    expect(await run(mutator()<undefined>('/api/items/x', { method: 'DELETE' }))).toBeUndefined();
    expect(await run(mutator()<string>('/api/ping'))).toBe('pong');
    expect(await run(mutator()<{ id: string }>('/api/items', { method: 'POST', body: '{}' }))).toEqual({ id: 'x' });
  });
});
