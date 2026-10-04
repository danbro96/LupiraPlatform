# Lupira.Identity.Marten.AspNetCore

`CurrentUser.GetAsync()` reads the request's `sub` / `email` / `name` claims and resolves them through `PrincipalDirectory` to the local `Principal`, JIT-provisioning on first login. With `StampProvenance`, it then stamps `EventActor` provenance on the request's session (`source` = `api`, or `dav` when the caller has no `sub`).

```csharp
builder.Services.AddLupiraCurrentUser();                                  // resolve only
builder.Services.AddLupiraCurrentUser(o => o.StampProvenance = true);    // resolve + stamp
```

Requires `AddLupiraPrincipalDirectory()` from `Lupira.Identity.Marten`. A principal subclass uses `AddLupiraCurrentUser<T>()` and `CurrentUser<T>`.
