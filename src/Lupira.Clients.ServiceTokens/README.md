# Lupira.Clients.ServiceTokens

Outbound auth for Lupira HTTP clients against Authentik: `TokenEndpointClient` (client credentials, RFC 8693 exchange, refresh), a single-flight `TokenCache`, and `ServiceTokenHandler`, which attaches a client-credentials bearer per hop, or the dev headers when the hop has no credentials. HttpClient only.

```csharp
builder.Services.Configure<AssistantOptions>(builder.Configuration.GetSection(AssistantOptions.SectionName));
builder.Services.AddHttpClient<AssistantClient>(c => c.BaseAddress = new Uri(baseUrl))
    .AddLupiraServiceToken<AssistantOptions>();   // AssistantOptions : IOutboundHopOptions
```

Fallback order when the hop has no `TokenUrl` + `ClientId` + `ClientSecret`: the request's `ServiceTokenProvider.DevUserOverride` as `X-Dev-User`, else `DevServiceId` as `X-Dev-Service`, else `DevUser` as `X-Dev-User` with `DevScopes` as `X-Dev-Scopes`. A failed mint throws `HttpRequestException` (inner `TokenEndpointException`).

Member token exchange (`ExchangeAsync` + `TokenCache.ExchangeKey`) and offline refresh (`RefreshAsync`) are building blocks; classifying the inbound caller stays in the host.
