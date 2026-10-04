# Lupira.Auth.Service

`Caller`: the authenticated caller as the service layer sees it — a member (`Email` + `Groups`, `IsAdmin`) or an internal service (`ServiceId`), with `Actor` for the event `actor` header. `AdminGroups.For("<app>")` gives the app's admin groups (`<app>-admins`, `platform-admins`). BCL only, so a domain project can take a `Caller`.

```csharp
static readonly string[] Admins = AdminGroups.For("assistant");
var caller = Caller.Member(email, groups, Admins);
```

The handler, options and `CurrentService` live in `Lupira.Auth.Service.AspNetCore`.
