import type * as AuthSession from 'expo-auth-session';
import type { TokenSet } from './oidc.ts';

export type OidcLoginStatus = 'loading' | 'ready' | 'unavailable' | 'busy';

export interface OidcLoginState {
  attempt: number;
  discovery: AuthSession.DiscoveryDocument | null;
  unavailable: boolean;
  busy: boolean;
  error: string | null;
}

export type OidcLoginEvent =
  | { type: 'retry' }
  | { type: 'discovered'; discovery: AuthSession.DiscoveryDocument }
  | { type: 'discoveryFailed'; error: string }
  | { type: 'signInStarted' }
  | { type: 'signInEnded'; error: string | null };

export const initialOidcLoginState: OidcLoginState = { attempt: 0, discovery: null, unavailable: false, busy: false, error: null };

export function oidcLoginReducer(state: OidcLoginState, event: OidcLoginEvent): OidcLoginState {
  switch (event.type) {
    case 'retry':
      return { ...state, attempt: state.attempt + 1, unavailable: false, error: null };
    case 'discovered':
      return { ...state, discovery: event.discovery, unavailable: false };
    case 'discoveryFailed':
      return { ...state, unavailable: true, error: event.error };
    case 'signInStarted':
      return { ...state, busy: true, error: null };
    case 'signInEnded':
      return { ...state, busy: false, error: event.error };
  }
}

export function oidcLoginStatus(state: OidcLoginState, requestReady: boolean): OidcLoginStatus {
  if (state.busy) return 'busy';
  if (state.unavailable) return 'unavailable';
  return state.discovery && requestReady ? 'ready' : 'loading';
}

export interface SignInSteps {
  prompt(): Promise<AuthSession.AuthSessionResult>;
  exchange(code: string): Promise<TokenSet>;
  onTokens(tokens: TokenSet): Promise<void>;
  log(tag: string, detail: string): void;
}

const message = (e: unknown) => (e instanceof Error ? e.message : String(e));

/** Resolves to the error to show; null on success or when the user backed out of the browser. */
export async function runSignIn({ prompt, exchange, onTokens, log }: SignInSteps): Promise<string | null> {
  try {
    const result = await prompt();
    log('auth', `sign-in ${result.type}`);
    if (result.type === 'error') return result.error?.message ?? 'Sign-in failed.';
    if (result.type !== 'success') return null;
    await onTokens(await exchange(result.params.code));
    log('auth', 'signed in');
    return null;
  } catch (e) {
    log('auth', `sign-in failed: ${message(e)}`);
    return message(e);
  }
}
