# Changelog

## 0.2.1
- The session cookie forwards its challenge to OIDC, so a policy naming the cookie scheme (the `RequireAuthenticatedFallback` policy) sends an anonymous page to Authentik instead of looping on `/Account/Login`.

## 0.2.0
- Breaking: `MapLupiraAuthEndpoints()` returns the `/auth` `RouteGroupBuilder`, so a consumer can add conventions such as `.WithTags(...)`.

## 0.1.1
- `/auth/login` rejects a return URL containing control characters.

## 0.1.0
- `AddLupiraBffAuth(Action<LupiraBffAuthOptions>)`: Production cookie (`__Host-` enforced, 8 h sliding, Lax, HttpOnly, Secure) + OIDC code/PKCE public client with optional secret and Duende token refresh; JwtBearer (`MapInboundClaims = false`, `email` / `groups`); 401/403 on API prefixes from the proxy surface; optional guest cookie + policy; optional fallback policy with a required group; `Admin` policy.
- `DevAuthHandler`: configured local user with parameterized default groups.
- `MapLupiraAuthEndpoints()`: `/auth/login?returnUrl` (same-site paths only), `POST /auth/logout`, `/auth/user` (`UserInfo`, operation `GetSession`).
