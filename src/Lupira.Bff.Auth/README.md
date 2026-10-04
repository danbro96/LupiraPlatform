# Lupira.Bff.Auth

Authentik front doors for Lupira BFFs and the `/auth` endpoints the SPA drives.

```csharp
builder.AddLupiraBffProxy();
builder.AddLupiraBffAuth(o =>
{
    o.EnableOidc = true;               // Production: cookie + OIDC code/PKCE, Duende refresh
    o.EnableBearer = true;             // JwtBearer when an authority is configured
    o.Audience = "lupira-cal";
    o.CookieName = "__Host-lupira-cal";
    o.AdminGroups = ["cal-admins", "platform-admins"];
    o.DevGroups = ["cal-admins"];
});

app.MapLupiraAuthEndpoints();          // GET /auth/login, POST /auth/logout, GET /auth/user
```

- Configuration overrides code: `Auth:Oidc:{Authority,ClientId,ClientSecret,Scopes}`, `Auth:Bearer:{Authority,Audience}`, `Auth:RequiredGroup`.
- Non-production authenticates every request as `Dev:User` with `Dev:Groups` (default `DevGroups`).
- API paths get 401/403, never a login redirect. Prefixes come from the proxy's `ExposedSurface`; set `ApiPrefixes` when there is no proxy.
- `Guest` adds a fixed-lifetime cookie scheme and policy requiring `RequiredClaim`.
- `RequireAuthenticatedFallback` (+ `RequiredGroup`) gates every endpoint without `AllowAnonymous`. `Admin` policy = any of `AdminGroups`.
