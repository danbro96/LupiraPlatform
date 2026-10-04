# Lupira.Hosting.OpenApi

OpenAPI document conventions for Lupira APIs and the Scalar UI.

- Security scheme (`Bearer`, `ApiKey` header or `Cookie`) and a security requirement on secured operations.
- `ProblemDetails` component; problem+json on 401 (secured), 500 (always) and every bodyless 4xx/5xx.
- Opt-in schema fixes: null stripped from shared enum schemas, `DateTimeOffset` forced to `string`/`date-time`.
- Optional `Idempotency-Key` header on operations carrying a marker.

```csharp
// Call inside the app's own AddOpenApi: the XML-comment source generator only intercepts that call.
builder.Services.AddOpenApi("v1", options => options.AddLupiraConventions(o =>
{
    o.Title = "Lupira Tasks API";
    o.Description = "…";
    o.DropNullEnumMembers = true;
    o.IdempotencyHeader<IdempotentMutation>("Client-generated GUIDv7 command id.");
    o.OperationTransformers.Add((operation, context, ct) => Task.CompletedTask); // runs before the built-in one
}));

app.MapLupiraOpenApi(o => o.Title = "Lupira Tasks API"); // /openapi/{documentName}.json, /scalar, / → /scalar
```

| Option | Default | |
|---|---|---|
| `Title` / `Description` | null | Replaces the info block (version = document name); null keeps the generator's. |
| `Security` | `ApiSecurity.Bearer()` | `ApiKeyHeader(name, description)`, `Cookie(name, description)`, or null for none. |
| `AuthDetection` | `AuthorizeData` | `NotAllowAnonymous` for apps secured by a fallback policy. |
| `UnauthorizedResponse` | true | 401 on secured operations. |
| `DropNullEnumMembers`, `DateTimeOffsetAsString` | false | Schema fixes. |
| `MapLupiraOpenApi`: `Title`, `Theme`, `RootRedirect`, `AllowAnonymous` | null, BluePlanet, true, true | |
