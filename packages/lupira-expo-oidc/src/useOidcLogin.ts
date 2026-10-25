import * as AuthSession from 'expo-auth-session';
import { useEffect, useReducer } from 'react';
import type { OidcClient, TokenSet } from './oidc.ts';
import { initialOidcLoginState, oidcLoginReducer, oidcLoginStatus, runSignIn, type OidcLoginStatus } from './oidcLogin.ts';

export interface OidcLoginOptions {
  oidc: Pick<OidcClient, 'clientId' | 'getDiscovery' | 'exchangeAuthCode'>;
  scopes: string[];
  scheme: string;
  path: string;
  onTokens(tokens: TokenSet): Promise<void>;
  log?: (tag: string, detail: string) => void;
}

export interface OidcLogin {
  status: OidcLoginStatus;
  error: string | null;
  retry(): void;
  signIn(): Promise<void>;
  redirectUri: string;
}

const noop = () => {};

/** PKCE code-flow login: discovery, the browser prompt and the code exchange; `onTokens` stores the session. */
export function useOidcLogin({ oidc, scopes, scheme, path, onTokens, log = noop }: OidcLoginOptions): OidcLogin {
  const redirectUri = AuthSession.makeRedirectUri({ scheme, path });
  const [state, dispatch] = useReducer(oidcLoginReducer, initialOidcLoginState);
  const { attempt, discovery } = state;

  useEffect(() => {
    let cancelled = false;
    oidc.getDiscovery().then(
      (d) => {
        if (!cancelled) dispatch({ type: 'discovered', discovery: d });
      },
      (e: unknown) => {
        if (!cancelled) dispatch({ type: 'discoveryFailed', error: e instanceof Error ? e.message : String(e) });
      },
    );
    return () => {
      cancelled = true;
    };
  }, [oidc, attempt]);

  const [request, , promptAsync] = AuthSession.useAuthRequest(
    { clientId: oidc.clientId, scopes, redirectUri, usePKCE: true, responseType: AuthSession.ResponseType.Code },
    discovery,
  );

  async function signIn() {
    if (!request || !discovery) return;
    dispatch({ type: 'signInStarted' });
    const error = await runSignIn({
      // createTask:false keeps the browser in the app's Android task; otherwise the redirect resolves 'dismiss' (expo/expo#23781).
      prompt: () => promptAsync({ createTask: false }),
      exchange: (code) =>
        oidc.exchangeAuthCode({ code, redirectUri, codeVerifier: request.codeVerifier, tokenEndpoint: discovery.tokenEndpoint }),
      onTokens,
      log,
    });
    dispatch({ type: 'signInEnded', error });
  }

  return {
    status: oidcLoginStatus(state, request !== null),
    error: state.error,
    retry: () => dispatch({ type: 'retry' }),
    signIn,
    redirectUri,
  };
}
