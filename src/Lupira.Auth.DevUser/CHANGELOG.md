# Changelog

## 0.1.0
- `AddLupiraDevHeaderAuth(scheme)`: `X-Dev-User` header scheme (email lower-cased; optional `X-Dev-Groups`, `X-Dev-Scopes`, `X-Dev-Client` → `groups`, `scope`, `azp`). Missing header → no result; empty header → failure.
- `AddLupiraDevConfigUserAuth(scheme, configure?)`: every request as `Dev:User` with `Dev:Groups`, falling back to `DefaultUser` / `DefaultGroups`.
- Both emit `sub` = `dev|<email>` and `email`, with name claim type `email` and role claim type `groups`.
