# Changelog

## 0.1.1
- `/auth/login` rejects a return URL containing control characters.

## 0.1.0
- `AddLupiraBffAuth(Action<LupiraBffAuthOptions>)`: Production cookie (`__Host-` enforced, 8 h sliding, Lax, HttpOnly, Secure) + OIDC code/PKCE public client with optional secret and Duende token refresh; JwtBearer (`MapInboundClaims = false`, `email` / `groups`); 401/403 on API prefixes from the proxy surface; optional guest cookie + policy; optional fallback policy with a required group; `Admin` policy.
- `DevAuthHandler`: configured local user with parameterized default groups.
- `MapLupiraAuthEndpoints()`: `/auth/login?returnUrl` (same-site paths only), `POST /auth/logout`, `/auth/user` (`UserInfo`, operation `GetSession`).
