# Lupira.Bff.OpenApi

Rebuilds a BFF's OpenAPI document from its committed upstream specs (embedded as `upstream/*.json`), keeping only the `documented` groups of `exposed.json`. Every `dotnet build` then writes the contract the clients generate from.

```csharp
builder.Services.AddLupiraBffOpenApi(o =>
{
    o.Title = "LupiraCal BFF";
    o.Version = "v1";                 // null: the first upstream's info.version
    o.RetagByCluster = true;
    o.NamespaceCollisions = true;
    o.Upstreams.Add(new UpstreamSpec { Cluster = "cal-api", Name = "LupiraCalApi" });
    o.SecuritySchemes["Cookie"] = BffSecuritySchemes.Cookie("__Host-lupira-cal", "Session cookie minted by the BFF's OIDC login.");
    o.SecuritySchemes["Bearer"] = BffSecuritySchemes.Bearer("Authentik access token from a native client.");
});
```

- `SecurityFor(operation)` picks scheme names per operation (default `Cookie`, `Bearer` as alternatives); a name missing from `SecuritySchemes` throws.
- A group's `pathMap` remounts its paths and drops the route parameters it removed.
- `SortPaths` orders paths ordinally.
