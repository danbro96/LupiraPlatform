# Lupira.Auth.Service.AspNetCore

Internal-hop service auth:

- `Service` scheme (`ServiceAuthHandler`): the `X-Dev-Service: <id>` header becomes a `service_id` claim, in Development only.
- `ServiceJwt` bearer: Authentik client-credentials tokens, registered only when `Auth:Service` (`Authority`, `Audience`) is configured.
- `Service` policy over both schemes; `CurrentService` reads the service id (`service_id`, then `azp`/`client_id`/`sub`) and returns `Caller.Service(id)`.

```csharp
var serviceOpts = builder.Configuration.GetSection(ServiceAuthOptions.SectionName).Get<ServiceAuthOptions>() ?? new();
builder.Services.AddAuthentication().AddLupiraServiceAuth(serviceOpts);
builder.Services.AddAuthorization(o => o.AddLupiraServicePolicy(serviceOpts));

app.MapPost("/ingest", ...).RequireAuthorization(ServiceAuthHandler.SchemeName);
```

`serviceOpts.AuthenticationSchemes` lists the active service schemes for policies that also admit members.
