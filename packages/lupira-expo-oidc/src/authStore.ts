import { setAuthPort, type AuthPort } from '@danbro96/lupira-http/authPort';
import * as SecureStore from 'expo-secure-store';
import { create, type StoreApi, type UseBoundStore } from 'zustand';
import { decodeJwt, type OidcClient, type RefreshError, type TokenSet } from './oidc.ts';
import { createTokenRefresher, secureSessionStore } from './tokenSession.ts';

export type AuthMode = 'oidc' | 'dev';

export interface AuthUser {
  /** Email when the token carries one, else the OIDC subject. */
  sub: string;
  name?: string;
}

export interface AuthState {
  /** Hydration gate: the app renders nothing until the persisted session is loaded. */
  loaded: boolean;
  apiUrl: string;
  authMode: AuthMode;
  token: string | null;
  refreshToken: string | null;
  /** Epoch ms; 0 = no session. */
  expiresAt: number;
  user: AuthUser | null;
}

export interface AuthActions {
  load(): Promise<void>;
  /** Clears the session: a token minted for one backend is meaningless against another. */
  setBackend(urls: Record<string, string>, authMode: AuthMode): Promise<void>;
  setSession(tokens: TokenSet): Promise<void>;
  /** `reason: 'expired'` = the refresher dropped the session; a deliberate sign-out passes none. */
  clearSession(opts?: { reason?: 'expired' }): Promise<void>;
  refreshIfNeeded(opts?: { force?: boolean; sentToken?: string }): Promise<string | null>;
  isAuthenticated(): boolean;
  /** Fires on a sign-in and on switching to the dev backend; returns the unsubscribe. */
  onSignIn(cb: () => void): () => void;
}

export type AuthStore = AuthState & AuthActions;

export interface AuthStoreOptions<X extends object> {
  /** SecureStore key prefix: `<prefix>.token|refreshToken|expiresAt|userSub|userName|lastSub|apiUrl|authMode`. */
  keyPrefix: string;
  /** Overrides for installs that already hold these values under other keys. */
  storageKeys?: { apiUrl?: string; authMode?: string };
  defaultApiUrl: string;
  defaultAuthMode: AuthMode;
  oidc: Pick<OidcClient, 'refreshTokens'>;
  log?: (tag: string, detail: string) => void;
  /** Awaited before a sign-in as a different account than the last one lands; a throw aborts the sign-in. */
  onAccountChange?(prevSub: string | null, nextSub: string): Promise<void> | void;
  /** Awaited while the session is still live, so a final authenticated call can go out. */
  beforeSignOut?(reason?: 'expired'): Promise<void> | void;
  onDefinitiveFailure?(e: RefreshError): void;
  /** Awaited inside `load()` before `loaded` flips; hydrates `extend` state. */
  onLoad?(): Promise<void>;
  extend?(set: StoreApi<AuthStore & X>['setState'], get: () => AuthStore & X): X;
}

const claim = (claims: Record<string, unknown>, ...names: string[]): string | undefined =>
  names.map((n) => claims[n]).find((v): v is string => typeof v === 'string');

function userFrom(accessToken: string): AuthUser | null {
  const claims = decodeJwt(accessToken);
  const sub = claim(claims, 'email', 'preferred_username', 'sub');
  return sub ? { sub, name: claim(claims, 'name', 'given_name') } : null;
}

const message = (e: unknown) => (e instanceof Error ? e.message : String(e));

/** The lupira-http AuthPort over an auth store; apps with a wider port spread it into theirs. */
export function toAuthPort(getState: () => AuthStore): AuthPort {
  return {
    getApiUrl: () => getState().apiUrl,
    getToken: () => (getState().authMode === 'dev' ? null : getState().token),
    refresh: (force, sentToken) => getState().refreshIfNeeded({ force, sentToken }),
    onSignIn: (cb) => getState().onSignIn(cb),
  };
}

/** The app's session store; registers itself as the lupira-http AuthPort. */
export function createAuthStore<X extends object = object>(options: AuthStoreOptions<X>): UseBoundStore<StoreApi<AuthStore & X>> {
  const { keyPrefix, defaultApiUrl, defaultAuthMode, log = () => {} } = options;
  const keys = {
    apiUrl: options.storageKeys?.apiUrl ?? `${keyPrefix}.apiUrl`,
    authMode: options.storageKeys?.authMode ?? `${keyPrefix}.authMode`,
    userSub: `${keyPrefix}.userSub`,
    userName: `${keyPrefix}.userName`,
    lastSub: `${keyPrefix}.lastSub`,
  };
  const sessionStore = secureSessionStore(keyPrefix);
  const put = (key: string, value: string | undefined) =>
    value ? SecureStore.setItemAsync(key, value) : SecureStore.deleteItemAsync(key);
  const signInListeners = new Set<() => void>();
  const notifySignIn = () => signInListeners.forEach((cb) => cb());
  // Outlives sign-out so a same-account re-login keeps the local data.
  let lastSub: string | null = null;

  const useAuth = create<AuthStore & X>()((set, get) => {
    const setAuth = set as (partial: Partial<AuthState>) => void;
    const base: AuthStore = {
      loaded: false,
      apiUrl: defaultApiUrl,
      authMode: defaultAuthMode,
      token: null,
      refreshToken: null,
      expiresAt: 0,
      user: null,

      async load() {
        const [apiUrl, authMode, userSub, userName, storedLastSub, session] = await Promise.all([
          SecureStore.getItemAsync(keys.apiUrl),
          SecureStore.getItemAsync(keys.authMode),
          SecureStore.getItemAsync(keys.userSub),
          SecureStore.getItemAsync(keys.userName),
          SecureStore.getItemAsync(keys.lastSub),
          sessionStore.load(),
          options.onLoad?.(),
        ]);
        // An install signed in before lastSub existed adopts its account instead of announcing a change.
        lastSub = storedLastSub ?? userSub;
        setAuth({
          loaded: true,
          apiUrl: apiUrl || defaultApiUrl,
          // 'none' is the pre-rename value of 'dev'.
          authMode: authMode === 'none' ? 'dev' : ((authMode as AuthMode | null) ?? defaultAuthMode),
          token: session.token,
          refreshToken: session.refreshToken,
          expiresAt: session.expiresAt,
          user: userSub ? { sub: userSub, name: userName || undefined } : null,
        });
      },

      async setBackend(urls, authMode) {
        await get().clearSession();
        setAuth({ apiUrl: urls.api, authMode });
        await Promise.all([SecureStore.setItemAsync(keys.apiUrl, urls.api), SecureStore.setItemAsync(keys.authMode, authMode)]);
        log('auth', `backend → ${urls.api} (${authMode})`);
        if (authMode === 'dev') notifySignIn();
      },

      async setSession(t) {
        const signingIn = get().token === null;
        const user = userFrom(t.accessToken) ?? get().user;
        const accountChanged = user !== null && user.sub !== lastSub;
        if (accountChanged) {
          await options.onAccountChange?.(lastSub, user.sub);
          lastSub = user.sub;
        }
        const expiresAt = Date.now() + (t.expiresIn ?? 3600) * 1000;
        const refreshToken = t.refreshToken ?? get().refreshToken;
        // In-memory first: a rotated refresh token must survive a persistence failure or the session is stranded.
        setAuth({ token: t.accessToken, refreshToken, expiresAt, user });
        try {
          await Promise.all([
            sessionStore.save({ token: t.accessToken, refreshToken, expiresAt }),
            put(keys.userSub, user?.sub),
            put(keys.userName, user?.name),
            accountChanged ? SecureStore.setItemAsync(keys.lastSub, user.sub) : undefined,
          ]);
        } catch (e) {
          log('auth:persist-error', message(e));
        }
        if (signingIn) notifySignIn();
      },

      async clearSession(opts) {
        await options.beforeSignOut?.(opts?.reason);
        setAuth({ token: null, refreshToken: null, expiresAt: 0, user: null });
        await Promise.all([sessionStore.clear(), SecureStore.deleteItemAsync(keys.userSub), SecureStore.deleteItemAsync(keys.userName)]);
      },

      refreshIfNeeded(opts) {
        return get().authMode === 'dev' ? Promise.resolve(get().token) : refresh(opts);
      },

      isAuthenticated() {
        const s = get();
        return s.authMode === 'dev' || s.token !== null;
      },

      onSignIn(cb) {
        signInListeners.add(cb);
        return () => signInListeners.delete(cb);
      },
    };
    return { ...base, ...(options.extend?.(set, get) as X) };
  });

  const refresh = createTokenRefresher({
    read: () => {
      const { token, refreshToken, expiresAt } = useAuth.getState();
      return { token, refreshToken, expiresAt };
    },
    refreshTokens: (refreshToken) => options.oidc.refreshTokens(refreshToken),
    apply: (t) => useAuth.getState().setSession(t),
    signOut: () => useAuth.getState().clearSession({ reason: 'expired' }),
    log,
    onDefinitiveFailure: options.onDefinitiveFailure,
  });

  setAuthPort(toAuthPort(useAuth.getState));
  return useAuth;
}
