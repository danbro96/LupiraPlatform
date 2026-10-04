# Lupira.Bff.Proxy

Allowlist reverse proxy for Lupira BFFs. The embedded `exposed.json` names every `VERB /path` the BFF forwards; routes are written to `ReverseProxy:Routes` as a configuration source, so `ReverseProxy:Clusters` stays in appsettings.

```csharp
if (builder.TryPrintLupiraBffRoutes(args)) return;   // --routes
builder.AddLupiraBffProxy();

app.UseLupiraBffDeviceKeyGate();   // before UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
app.MapLupiraBffProxy();           // MapReverseProxy + API prefix fence (404)
app.MapFallbackToFile("index.html");
```

## exposed.json

```json
{
  "clusters": {
    "cal-api": { "prefix": "/api" },
    "assistant-api": { "prefix": "/api", "announcePrefix": true }
  },
  "groups": {
    "static": { "catchAll": true },
    "device": { "policy": "Anonymous", "prefixed": false, "credential": "deviceKey" },
    "guest": {
      "policy": "Guest", "documented": true, "credential": "none",
      "pathMap": { "upstream": "/shared/{token}", "bff": "/share", "claims": { "token": "share-token" } }
    }
  },
  "operations": { "cal-api": ["GET /items", "POST /items"] },
  "static": { "geo-api": ["GET /basemap/{**path}"] },
  "device": { "location-api": ["POST /ingest/location"] }
}
```

- Every top-level key other than `clusters` and `groups` is a group: cluster → entries. `operations` is implicit (`Default`, documented); any other group must be declared.
- Group fields: `policy` (`Default`), `prefixed` (`true`), `catchAll` (`false`; GET-only, `{**name}` last), `documented` (`false`), `credential` (`session` | `deviceKey` | `none`), `pathMap`.
- `session`: a caller's `Bearer` passes verbatim; Development replaces `X-Dev-User`; otherwise the Duende user access token. `deviceKey`: well-formedness gate, forwarded untouched. `none`: `Authorization` and `X-Dev-User` stripped.
- A `{**catch-all}` outside a catch-all group, an unknown group property, a duplicate entry or a route-key collision throws at startup.

`SessionTokenHandler` applies the `session` rule to typed `HttpClient`s.
