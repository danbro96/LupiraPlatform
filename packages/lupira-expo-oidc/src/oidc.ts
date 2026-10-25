import type * as AuthSession from 'expo-auth-session';

export interface TokenSet {
  accessToken: string;
  refreshToken?: string;
  idToken?: string;
  expiresIn?: number;
}

/**
 * A refresh attempt failed. `definitive` = the refresh token / client was rejected by the
 * server (re-auth required); otherwise the failure is transient (network/timeout/5xx) and the
 * session should be kept and retried later.
 */
export class RefreshError extends Error {
  readonly definitive: boolean;

  constructor(definitive: boolean, message: string) {
    super(message);
    this.definitive = definitive;
    this.name = 'RefreshError';
  }
}

export class DiscoveryError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DiscoveryError';
  }
}

export interface OidcClientOptions {
  issuer: string;
  clientId: string;
  timeoutMs: number;
  log?: (tag: string, detail: string) => void;
}

export interface OidcClient {
  readonly clientId: string;
  getDiscovery(): Promise<AuthSession.DiscoveryDocument>;
  exchangeAuthCode(params: { code: string; redirectUri: string; codeVerifier?: string; tokenEndpoint?: string }): Promise<TokenSet>;
  refreshTokens(refreshToken: string): Promise<TokenSet>;
}

const message = (e: unknown) => (e instanceof Error ? e.message : String(e));

function toTokenSet(text: string): TokenSet {
  const json = JSON.parse(text) as { access_token: string; refresh_token?: string; id_token?: string; expires_in?: number };
  return { accessToken: json.access_token, refreshToken: json.refresh_token, idToken: json.id_token, expiresIn: json.expires_in };
}

/** Non-hook OIDC helpers (the interactive login itself stays with expo-auth-session's useAuthRequest). */
export function createOidcClient({ issuer, clientId, timeoutMs, log = () => {} }: OidcClientOptions): OidcClient {
  async function fetchText(endpoint: string, init: RequestInit): Promise<{ status: number; text: string }> {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeoutMs);
    try {
      const res = await fetch(endpoint, { ...init, signal: controller.signal });
      return { status: res.status, text: await res.text() };
    } finally {
      clearTimeout(timer);
    }
  }

  /**
   * POST an `application/x-www-form-urlencoded` body to a token endpoint with a bounded timeout.
   * Returns the raw `{ status, text }` for any HTTP response (so callers branch on status);
   * throws only on a transport failure (network down / timeout / connection dropped mid-body).
   */
  function postForm(endpoint: string, params: Record<string, string>): Promise<{ status: number; text: string }> {
    return fetchText(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded', Accept: 'application/json' },
      body: new URLSearchParams(params).toString(),
    });
  }

  // expo's fetchDiscoveryAsync calls response.json() on whatever comes back, so an empty body surfaces as "JSON Parse error".
  async function fetchDiscovery(): Promise<AuthSession.DiscoveryDocument> {
    const url = `${issuer.replace(/\/+$/, '')}/.well-known/openid-configuration`;
    const { status, text } = await fetchText(url, { headers: { Accept: 'application/json' } });
    log('discovery:response', `status=${status} len=${text.length}`);
    if (status < 200 || status >= 300) throw new DiscoveryError(`discovery ${status}: ${text.slice(0, 200)}`);
    if (!text) throw new DiscoveryError(`discovery ${status}: empty body`);
    let json: unknown;
    try {
      json = JSON.parse(text);
    } catch {
      json = null;
    }
    if (typeof json !== 'object' || json === null) throw new DiscoveryError(`discovery ${status}: not a JSON object: ${text.slice(0, 200)}`);
    const doc = json as AuthSession.ProviderMetadata;
    return {
      discoveryDocument: doc,
      authorizationEndpoint: doc.authorization_endpoint,
      tokenEndpoint: doc.token_endpoint,
      revocationEndpoint: doc.revocation_endpoint,
      userInfoEndpoint: doc.userinfo_endpoint,
      endSessionEndpoint: doc.end_session_endpoint,
      registrationEndpoint: doc.registration_endpoint,
    };
  }

  let discoveryPromise: Promise<AuthSession.DiscoveryDocument> | null = null;

  function getDiscovery(): Promise<AuthSession.DiscoveryDocument> {
    // Don't cache a failure: a single failed discovery (e.g. a launch-time network blip) must
    // not poison the cache for the whole session — null it out so the next call retries.
    discoveryPromise ??= fetchDiscovery().catch((e: unknown) => {
      discoveryPromise = null;
      throw e;
    });
    return discoveryPromise;
  }

  /**
   * Exchange an authorization code for tokens with a manual fetch (PKCE public client) so the
   * raw HTTP status + body are visible in the auth trace — expo-auth-session's exchangeCodeAsync
   * hides them and surfaces only "JSON Parse error" when the token endpoint returns a non-JSON
   * or empty body.
   */
  async function exchangeAuthCode(params: { code: string; redirectUri: string; codeVerifier?: string; tokenEndpoint?: string }): Promise<TokenSet> {
    const tokenEndpoint = params.tokenEndpoint ?? (await getDiscovery()).tokenEndpoint;
    if (!tokenEndpoint) throw new Error('discovery: no token endpoint');
    const form: Record<string, string> = {
      grant_type: 'authorization_code',
      code: params.code,
      redirect_uri: params.redirectUri,
      client_id: clientId,
    };
    if (params.codeVerifier) form.code_verifier = params.codeVerifier;

    log('token:post', tokenEndpoint);
    const { status, text } = await postForm(tokenEndpoint, form);
    const failed = status < 200 || status >= 300;
    log('token:response', `status=${status} len=${text.length}${failed ? ` body=${text.slice(0, 400)}` : ''}`);
    if (failed) throw new Error(`token ${status}: ${text.slice(0, 400)}`);
    if (!text) throw new Error(`token ${status}: empty body`);
    return toTokenSet(text);
  }

  /**
   * Exchange a refresh token for a fresh access token (+ rotated refresh token) via a manual POST
   * (mirrors exchangeAuthCode for full status visibility). Throws a RefreshError whose `definitive`
   * flag tells the caller whether to drop the session (grant rejected) or keep it and retry later
   * (transient network/server failure).
   */
  async function refreshTokens(refreshToken: string): Promise<TokenSet> {
    let discovery: AuthSession.DiscoveryDocument;
    try {
      discovery = await getDiscovery();
    } catch (e) {
      throw new RefreshError(false, `discovery: ${message(e)}`);
    }
    const tokenEndpoint = discovery.tokenEndpoint;
    if (!tokenEndpoint) throw new RefreshError(false, 'discovery: no token endpoint');

    let status: number;
    let text: string;
    try {
      ({ status, text } = await postForm(tokenEndpoint, {
        grant_type: 'refresh_token',
        refresh_token: refreshToken,
        client_id: clientId,
      }));
    } catch (e) {
      throw new RefreshError(false, `network: ${message(e)}`);
    }

    // On failure include a body slice — it carries the rejection reason (e.g. invalid_grant),
    // which is the difference between "token expired/revoked" and "rotation replay".
    const failBody = status < 200 || status >= 300 ? ` body=${text.slice(0, 200)}` : '';
    log('refresh:response', `status=${status} len=${text.length}${failBody}`);
    // 400/401 = the refresh token or client was rejected → definitive, must re-authenticate.
    if (status === 400 || status === 401) throw new RefreshError(true, `refresh ${status}: ${text.slice(0, 200)}`);
    if (status < 200 || status >= 300) throw new RefreshError(false, `refresh ${status}`);
    if (!text) throw new RefreshError(false, 'refresh: empty body');
    return toTokenSet(text);
  }

  return { clientId, getDiscovery, exchangeAuthCode, refreshTokens };
}

/** Decode a JWT payload (no signature check — the server verifies; this is for claims only). */
export function decodeJwt(token: string): Record<string, unknown> {
  const payload = token.split('.')[1];
  if (!payload) return {};
  const b64 = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=');
  try {
    const bytes = globalThis.atob(b64);
    const json = decodeURIComponent(
      Array.prototype.map.call(bytes, (c: string) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(''),
    );
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return {};
  }
}

export function hasAudience(token: string | null, audience: string): boolean {
  if (!token) return false;
  const aud = decodeJwt(token).aud;
  return Array.isArray(aud) ? aud.includes(audience) : aud === audience;
}
