import * as SecureStore from 'expo-secure-store';
import { RefreshError, type TokenSet } from './oidc.ts';

export interface StoredSession {
  token: string | null;
  refreshToken: string | null;
  /** Epoch ms. */
  expiresAt: number;
}

export interface SessionStore {
  load(): Promise<StoredSession>;
  save(session: StoredSession): Promise<void>;
  clear(): Promise<void>;
}

export function secureSessionStore(prefix: string): SessionStore {
  const keys = { token: `${prefix}.token`, refreshToken: `${prefix}.refreshToken`, expiresAt: `${prefix}.expiresAt` };
  const put = (key: string, value: string | null) =>
    value ? SecureStore.setItemAsync(key, value) : SecureStore.deleteItemAsync(key);
  return {
    async load() {
      const [token, refreshToken, expiresAt] = await Promise.all(Object.values(keys).map((k) => SecureStore.getItemAsync(k)));
      return { token: token || null, refreshToken: refreshToken || null, expiresAt: expiresAt ? Number(expiresAt) : 0 };
    },
    async save(s) {
      await Promise.all([put(keys.token, s.token), put(keys.refreshToken, s.refreshToken), put(keys.expiresAt, s.expiresAt ? String(s.expiresAt) : null)]);
    },
    async clear() {
      await Promise.all(Object.values(keys).map((k) => SecureStore.deleteItemAsync(k)));
    },
  };
}

export interface TokenRefresherOptions {
  read(): StoredSession;
  refreshTokens(refreshToken: string): Promise<TokenSet>;
  apply(tokens: TokenSet, refreshToken: string): Promise<void>;
  signOut(): Promise<void>;
  marginMs?: number;
  log?: (tag: string, detail: string) => void;
  onDefinitiveFailure?: (e: RefreshError) => void;
}

export type RefreshIfNeeded = (opts?: { force?: boolean; sentToken?: string }) => Promise<string | null>;

/** Coalesced, rotation-safe refresh. */
export function createTokenRefresher({ read, refreshTokens, apply, signOut, marginMs = 60_000, log = () => {}, onDefinitiveFailure }: TokenRefresherOptions): RefreshIfNeeded {
  // Single-flight: the first refresher owns the POST, concurrent callers await the same promise. With
  // Authentik refresh-token rotation, a second concurrent POST replays an already-rotated token and the
  // provider treats it as theft — forced logout.
  let refreshing: Promise<string | null> | null = null;

  return async (opts) => {
    const { token, refreshToken, expiresAt } = read();
    if (!token) return null;

    // Another caller already rotated past the token this 401 was about — don't rotate again.
    if (opts?.force && opts.sentToken && opts.sentToken !== token) return token;

    const fresh = Date.now() < expiresAt - marginMs;
    if (fresh && !opts?.force) return token;

    if (!refreshToken) {
      if (opts?.force) {
        log('auth', 'forced refresh with no refresh token — signing out');
        await signOut();
        return null;
      }
      return token;
    }

    if (refreshing) return refreshing;
    refreshing = (async () => {
      try {
        const t = await refreshTokens(refreshToken);
        if (!t.accessToken) return token;
        await apply(t, refreshToken);
        log('auth', 'token refreshed');
        return read().token;
      } catch (e) {
        if (e instanceof RefreshError && e.definitive) {
          log('auth', `definitive refresh failure — signing out (${e.message})`);
          onDefinitiveFailure?.(e);
          await signOut();
          return null;
        }
        log('auth', `transient refresh failure — keeping session (${String(e)})`);
        return read().token;
      }
    })().finally(() => {
      refreshing = null;
    });
    return refreshing;
  };
}
