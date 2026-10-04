# Lupira.Identity.Marten

Per-service identity on Marten, with no ASP.NET dependency:

- `Principal`: the JIT-provisioned identity document (`mt_doc_principal`). `AuthentikSub` is the durable anchor, `Email` the mutable join key.
- `PrincipalDirectory`: `ResolveOrProvisionAsync(sub, email, name)` (sub first, then email; a unique `AuthentikSub` index settles concurrent first logins), `FindByEmailAsync`, `FindBySubAsync`, `LookupAsync(ids)`.
- `EventActor`: `Of(event)` reads the `actor` header; `Stamp(session, principal, source)` stamps `LastModifiedBy`, `actor.email`, `source` and trace/span correlation/causation; `Stamp(session, actor, actorEmail, commandId, source)` also stamps the `actor` header with the command id as causation.

```csharp
opts.AddLupiraPrincipals();                 // StoreOptions: unique AuthentikSub + Email indexes
services.AddLupiraPrincipalDirectory();     // scoped PrincipalDirectory
```

A service whose principal carries extra fields subclasses `Principal` with a class also named `Principal` (so the document alias stays `principal`) and uses `AddLupiraPrincipals<T>()`, `PrincipalDirectory<T>` and `AddLupiraPrincipalDirectory<T>()`.

`CurrentUser` lives in `Lupira.Identity.Marten.AspNetCore`.
