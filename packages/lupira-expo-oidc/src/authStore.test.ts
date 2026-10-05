import { beforeEach, describe, expect, it, vi } from 'vitest';

const store = new Map<string, string>();
const failWrites = { on: false };
vi.mock('expo-secure-store', () => ({
  getItemAsync: vi.fn((k: string) => Promise.resolve(store.get(k) ?? null)),
  setItemAsync: vi.fn((k: string, v: string) => {
    if (failWrites.on) return Promise.reject(new Error('keystore unavailable'));
    store.set(k, v);
    return Promise.resolve();
  }),
  deleteItemAsync: vi.fn((k: string) => {
    store.delete(k);
    return Promise.resolve();
  }),
}));
vi.mock('expo-auth-session', () => ({ fetchDiscoveryAsync: vi.fn() }));

import { authPort } from '@danbro96/lupira-http/authPort';
import { createAuthStore, type AuthStoreOptions } from './authStore.ts';
import { RefreshError, type TokenSet } from './oidc.ts';

const refreshTokens = vi.fn<(rt: string) => Promise<TokenSet>>();
const jwt = (claims: Record<string, unknown>) => `h.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.s`;
const tokenFor = (email: string, name?: string) => jwt({ email, name });

function make<X extends object = object>(options: Partial<AuthStoreOptions<X>> = {}) {
  return createAuthStore<X>({
    keyPrefix: 'lupira.test',
    defaultApiUrl: 'https://api.test',
    defaultAuthMode: 'oidc',
    oidc: { refreshTokens },
    ...options,
  });
}

type Store = ReturnType<typeof make>;

function seed(useAuth: Store, expiresInMs: number) {
  useAuth.setState({ loaded: true, authMode: 'oidc', token: 'tok-1', refreshToken: 'rt-1', expiresAt: Date.now() + expiresInMs, user: { sub: 'user@test' } });
}

function deferred<T>() {
  let resolve!: (v: T) => void;
  const promise = new Promise<T>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

beforeEach(() => {
  store.clear();
  failWrites.on = false;
  refreshTokens.mockReset();
});

describe('refreshIfNeeded', () => {
  it('stands pat on a fresh token', async () => {
    const useAuth = make();
    seed(useAuth, 3_600_000);
    expect(await useAuth.getState().refreshIfNeeded()).toBe('tok-1');
    expect(refreshTokens).not.toHaveBeenCalled();
  });

  it('coalesces concurrent refreshes near expiry into one token-endpoint call', async () => {
    const useAuth = make();
    seed(useAuth, 10_000);
    const reply = deferred<TokenSet>();
    refreshTokens.mockReturnValue(reply.promise);

    const a = useAuth.getState().refreshIfNeeded();
    const b = useAuth.getState().refreshIfNeeded();
    reply.resolve({ accessToken: 'tok-2', refreshToken: 'rt-2', expiresIn: 3600 });

    expect([await a, await b]).toEqual(['tok-2', 'tok-2']);
    expect(refreshTokens).toHaveBeenCalledTimes(1);
    expect(store.get('lupira.test.token')).toBe('tok-2');
  });

  it('does not rotate again for a 401 about an already-replaced token', async () => {
    const useAuth = make();
    seed(useAuth, 3_600_000);
    useAuth.setState({ token: 'tok-2', refreshToken: 'rt-2' });

    expect(await useAuth.getState().refreshIfNeeded({ force: true, sentToken: 'tok-1' })).toBe('tok-2');
    expect(refreshTokens).not.toHaveBeenCalled();
  });

  it('coalesces concurrent forced refreshes and lets a late 401 on the old token skip rotation', async () => {
    const useAuth = make();
    seed(useAuth, 3_600_000);
    const reply = deferred<TokenSet>();
    refreshTokens.mockReturnValue(reply.promise);

    const a = useAuth.getState().refreshIfNeeded({ force: true, sentToken: 'tok-1' });
    const b = useAuth.getState().refreshIfNeeded({ force: true, sentToken: 'tok-1' });
    reply.resolve({ accessToken: 'tok-2', refreshToken: 'rt-2', expiresIn: 3600 });
    expect([await a, await b]).toEqual(['tok-2', 'tok-2']);

    expect(await useAuth.getState().refreshIfNeeded({ force: true, sentToken: 'tok-1' })).toBe('tok-2');
    expect(refreshTokens).toHaveBeenCalledTimes(1);
    expect(refreshTokens).toHaveBeenCalledWith('rt-1');
  });

  it('signs out as expired on a definitive failure, keeping only the last account', async () => {
    const beforeSignOut = vi.fn();
    const onDefinitiveFailure = vi.fn();
    const useAuth = make({ beforeSignOut, onDefinitiveFailure });
    useAuth.setState({ loaded: true });
    await useAuth.getState().setSession({ accessToken: tokenFor('user@test'), refreshToken: 'rt-1', expiresIn: 10 });
    refreshTokens.mockRejectedValue(new RefreshError(true, 'invalid_grant'));

    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBeNull();
    expect(useAuth.getState()).toMatchObject({ token: null, refreshToken: null, expiresAt: 0, user: null });
    expect([...store.keys()]).toEqual(['lupira.test.lastSub']);
    expect(beforeSignOut).toHaveBeenCalledWith('expired');
    expect(onDefinitiveFailure).toHaveBeenCalledOnce();
  });

  it('signs out on a forced refresh with no refresh token, but not on a proactive one', async () => {
    const useAuth = make();
    seed(useAuth, 10_000);
    useAuth.setState({ refreshToken: null });

    expect(await useAuth.getState().refreshIfNeeded()).toBe('tok-1');
    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBeNull();
    expect(useAuth.getState().token).toBeNull();
    expect(refreshTokens).not.toHaveBeenCalled();
  });

  it('keeps the session on a transient failure', async () => {
    const beforeSignOut = vi.fn();
    const useAuth = make({ beforeSignOut });
    seed(useAuth, 10_000);
    refreshTokens.mockRejectedValue(new RefreshError(false, '503'));

    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBe('tok-1');
    expect(useAuth.getState()).toMatchObject({ token: 'tok-1', refreshToken: 'rt-1', user: { sub: 'user@test' } });
    expect(beforeSignOut).not.toHaveBeenCalled();
  });

  it('allows a new refresh after a failed one settles', async () => {
    const useAuth = make();
    seed(useAuth, 10_000);
    refreshTokens.mockRejectedValueOnce(new RefreshError(false, '503'));
    refreshTokens.mockResolvedValueOnce({ accessToken: 'tok-2', refreshToken: 'rt-2', expiresIn: 3600 });

    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBe('tok-1');
    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBe('tok-2');
    expect(refreshTokens).toHaveBeenCalledTimes(2);
  });

  it('keeps the current token when the response carries no access token', async () => {
    const useAuth = make();
    seed(useAuth, 3_600_000);
    refreshTokens.mockResolvedValue({ accessToken: '' });

    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBe('tok-1');
    expect(useAuth.getState().token).toBe('tok-1');
  });

  it('keeps the previous refresh token when the endpoint issues none', async () => {
    const useAuth = make();
    seed(useAuth, 10_000);
    refreshTokens.mockResolvedValue({ accessToken: 'tok-2', refreshToken: null as unknown as undefined, expiresIn: 3600 });

    await useAuth.getState().refreshIfNeeded({ force: true });
    expect(useAuth.getState().refreshToken).toBe('rt-1');
  });

  it('sends nothing in dev auto-auth mode', async () => {
    const useAuth = make();
    useAuth.setState({ loaded: true, authMode: 'dev' });
    expect(await useAuth.getState().refreshIfNeeded({ force: true })).toBeNull();
    expect(refreshTokens).not.toHaveBeenCalled();
  });
});

describe('session persistence', () => {
  it('stores the session and account under the key prefix', async () => {
    const useAuth = make();
    await useAuth.getState().setSession({ accessToken: tokenFor('user@test'), refreshToken: 'rt-9', expiresIn: 3600 });

    expect([...store.keys()].sort()).toEqual([
      'lupira.test.expiresAt',
      'lupira.test.lastSub',
      'lupira.test.refreshToken',
      'lupira.test.token',
      'lupira.test.userSub',
    ]);
    expect(store.get('lupira.test.refreshToken')).toBe('rt-9');
    expect(store.get('lupira.test.userSub')).toBe('user@test');
  });

  it('round-trips a session through load', async () => {
    await make().getState().setSession({ accessToken: tokenFor('user@test', 'User'), refreshToken: 'rt-9', expiresIn: 3600 });

    const useAuth = make();
    await useAuth.getState().load();
    expect(useAuth.getState()).toMatchObject({ loaded: true, refreshToken: 'rt-9', user: { sub: 'user@test', name: 'User' } });
    expect(useAuth.getState().expiresAt).toBeGreaterThan(Date.now());
  });

  it('keeps the in-memory session when persistence fails', async () => {
    const log = vi.fn();
    const useAuth = make({ log });
    failWrites.on = true;

    await expect(useAuth.getState().setSession({ accessToken: 'tok-2', refreshToken: 'rt-2' })).resolves.toBeUndefined();
    expect(useAuth.getState()).toMatchObject({ token: 'tok-2', refreshToken: 'rt-2' });
    expect(log).toHaveBeenCalledWith('auth:persist-error', 'keystore unavailable');
  });

  it('a deliberate sign-out passes no reason and clears the stored session', async () => {
    const beforeSignOut = vi.fn();
    const useAuth = make({ beforeSignOut });
    await useAuth.getState().setSession({ accessToken: tokenFor('user@test'), refreshToken: 'rt-1' });

    await useAuth.getState().clearSession();
    expect(beforeSignOut).toHaveBeenCalledWith(undefined);
    expect(useAuth.getState().isAuthenticated()).toBe(false);
    expect([...store.keys()]).toEqual(['lupira.test.lastSub']);
  });

  it('runs beforeSignOut while the token is still live', async () => {
    const seen: (string | null)[] = [];
    const useAuth = make({ beforeSignOut: () => void seen.push(useAuth.getState().token) });
    seed(useAuth, 3_600_000);

    await useAuth.getState().clearSession();
    expect(seen).toEqual(['tok-1']);
  });
});

describe('load', () => {
  it('defaults on a fresh install', async () => {
    const useAuth = make();
    await useAuth.getState().load();
    expect(useAuth.getState()).toMatchObject({ loaded: true, apiUrl: 'https://api.test', authMode: 'oidc', token: null, user: null });
  });

  it("reads the pre-rename 'none' auth mode as dev", async () => {
    store.set('lupira.test.authMode', 'none');
    const useAuth = make();
    await useAuth.getState().load();
    expect(useAuth.getState().authMode).toBe('dev');
  });

  it('awaits onLoad before opening the hydration gate', async () => {
    const hydrated = deferred<void>();
    const useAuth = make({ onLoad: () => hydrated.promise });
    const loading = useAuth.getState().load();
    await Promise.resolve();
    expect(useAuth.getState().loaded).toBe(false);
    hydrated.resolve();
    await loading;
    expect(useAuth.getState().loaded).toBe(true);
  });
});

describe('setBackend', () => {
  it('applies the url and mode, clears the session, and survives a relaunch', async () => {
    const useAuth = make();
    seed(useAuth, 3_600_000);

    await useAuth.getState().setBackend({ api: 'http://10.0.2.2:5000' }, 'dev');
    expect(useAuth.getState()).toMatchObject({ apiUrl: 'http://10.0.2.2:5000', authMode: 'dev', token: null });

    const relaunched = make();
    await relaunched.getState().load();
    expect(relaunched.getState()).toMatchObject({ apiUrl: 'http://10.0.2.2:5000', authMode: 'dev' });
    expect(relaunched.getState().isAuthenticated()).toBe(true);
  });

  it('honours storage key overrides', async () => {
    const useAuth = make({ storageKeys: { apiUrl: 'legacy.apiUrl', authMode: 'legacy.authMode' } });
    await useAuth.getState().setBackend({ api: 'https://other.test' }, 'oidc');
    expect(store.get('legacy.apiUrl')).toBe('https://other.test');
    expect(store.get('legacy.authMode')).toBe('oidc');
  });
});

describe('onSignIn', () => {
  it('fires on a sign-in and a switch to dev, not on a token rotation', async () => {
    const useAuth = make();
    const cb = vi.fn();
    const off = useAuth.getState().onSignIn(cb);

    await useAuth.getState().setSession({ accessToken: tokenFor('user@test'), refreshToken: 'rt-1' });
    await useAuth.getState().setSession({ accessToken: tokenFor('user@test'), refreshToken: 'rt-2' });
    expect(cb).toHaveBeenCalledTimes(1);

    await useAuth.getState().setBackend({ api: 'http://dev.test' }, 'dev');
    expect(cb).toHaveBeenCalledTimes(2);

    off();
    await useAuth.getState().setBackend({ api: 'http://dev.test' }, 'dev');
    expect(cb).toHaveBeenCalledTimes(2);
  });
});

describe('onAccountChange', () => {
  it('announces the first account, then only a different one, before sign-in listeners run', async () => {
    const order: string[] = [];
    const onAccountChange = vi.fn((prev: string | null, next: string) => void order.push(`change ${prev}→${next}`));
    const useAuth = make({ onAccountChange });
    useAuth.getState().onSignIn(() => order.push('signed in'));

    await useAuth.getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-1' });
    await useAuth.getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-2' });
    await useAuth.getState().clearSession();
    await useAuth.getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-3' });
    await useAuth.getState().clearSession();
    await useAuth.getState().setSession({ accessToken: tokenFor('b@test'), refreshToken: 'rt-4' });

    expect(order).toEqual(['change null→a@test', 'signed in', 'signed in', 'change a@test→b@test', 'signed in']);
  });

  it('remembers the last account across a relaunch', async () => {
    await make().getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-1' });
    await make().getState().clearSession();

    const onAccountChange = vi.fn();
    const useAuth = make({ onAccountChange });
    await useAuth.getState().load();
    await useAuth.getState().setSession({ accessToken: tokenFor('b@test'), refreshToken: 'rt-2' });
    expect(onAccountChange).toHaveBeenCalledWith('a@test', 'b@test');
  });

  it('adopts the signed-in account of an install that predates lastSub', async () => {
    store.set('lupira.test.userSub', 'a@test');
    store.set('lupira.test.token', 'tok-1');
    const onAccountChange = vi.fn();
    const useAuth = make({ onAccountChange });
    await useAuth.getState().load();

    await useAuth.getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-2' });
    expect(onAccountChange).not.toHaveBeenCalled();
  });

  it('a throw aborts the sign-in and announces the change again next time', async () => {
    const onAccountChange = vi.fn().mockRejectedValueOnce(new Error('disk full'));
    const useAuth = make({ onAccountChange });

    await expect(useAuth.getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-1' })).rejects.toThrow('disk full');
    expect(useAuth.getState().token).toBeNull();

    await useAuth.getState().setSession({ accessToken: tokenFor('a@test'), refreshToken: 'rt-1' });
    expect(onAccountChange).toHaveBeenLastCalledWith(null, 'a@test');
    expect(useAuth.getState().user).toEqual({ sub: 'a@test' });
  });
});

describe('extend', () => {
  it('merges typed app state and actions that can reach the core', async () => {
    const useAuth = make<{ principalId: string | null; adopt(id: string): void }>({
      extend: (set, get) => ({
        principalId: null,
        adopt: (id) => set({ principalId: get().user ? id : null }),
      }),
    });
    seed(useAuth, 3_600_000);

    useAuth.getState().adopt('p-1');
    expect(useAuth.getState().principalId).toBe('p-1');
  });
});

describe('AuthPort', () => {
  it('registers the store as the lupira-http AuthPort', async () => {
    const useAuth = make();
    seed(useAuth, 3_600_000);
    expect(authPort().getApiUrl()).toBe('https://api.test');
    expect(authPort().getToken()).toBe('tok-1');
    expect(await authPort().refresh()).toBe('tok-1');

    useAuth.setState({ authMode: 'dev' });
    expect(authPort().getToken()).toBeNull();
  });
});
