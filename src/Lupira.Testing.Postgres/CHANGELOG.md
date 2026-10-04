# Changelog

## 0.1.0
- `LupiraApiFactory<TProgram>`: `WebApplicationFactory` over a Testcontainers Postgres (`image` ctor argument, `PostgresImages`), `Development` environment, `ConnectionStrings:<ConnectionStringName>` and optional `Auth:Oidc:Authority` (`AuthentikSlug`) plus `AddSettings` keys applied as host settings and in-memory configuration; `ResetAsync` runs `ApplySchemaAsync` once, then `ResetDataAsync`; container disposed with the factory.
- `DevClientExtensions` on any `WebApplicationFactory<T>`: `ApiClient(email, groups)` (`X-Dev-User`, `X-Dev-Groups`), `ScopedClient(email, scopes)` (`X-Dev-Scopes`), `ServiceClient(serviceId)` (`X-Dev-Service`), `DeviceKeyClient(apiKey)` (`Authorization: DeviceKey`), `AnonymousClient()`.
