# Lupira.Auth.DevUser

Development-only authentication: an `X-Dev-User` header scheme for APIs and a configured local user (`Dev:User` / `Dev:Groups`) for BFFs.

```csharp
if (builder.Environment.IsDevelopment())
    auth.AddLupiraDevHeaderAuth();

services.AddAuthentication("Dev").AddLupiraDevConfigUserAuth(configure: o => o.DefaultGroups = ["cal-admins"]);
```
