# Changelog

## 0.1.0
- `OpenApiOptions.AddLupiraConventions(o => …)`: info block, security scheme (`ApiSecurity.Bearer/ApiKeyHeader/Cookie`), `AuthDetection`, `ProblemDetails` component with problem+json 401/500/bodyless 4xx–5xx responses, `DropNullEnumMembers`, `DateTimeOffsetAsString`, `IdempotencyHeader<TMarker>`, ordered `OperationTransformers`.
- `MapLupiraOpenApi(o => …)`: `/openapi/{documentName}.json`, Scalar at `/scalar` (title, theme), `/` → `/scalar` redirect, anonymous by default.
