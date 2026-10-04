# Lupira.Auth.Jwt

OIDC JWT bearer authentication for Lupira APIs (Authentik): `Auth:Oidc` binding with a startup guard, the `/mcp` RFC 9728 challenge, the Development dev-header scheme (`Lupira.Auth.DevUser`), an optional `DevOrJwt` default scheme, and the shared policies.

```csharp
builder.AddLupiraJwt();
var apiSchemes = LupiraJwtSchemes.Api(builder.Environment);
builder.Services.AddAuthorizationBuilder()
    .AddLupiraApiPolicy(apiSchemes)
    .AddLupiraGatewayAzpPolicy(apiSchemes, builder.Configuration["DavGateway:ClientId"])
    .AddLupiraInternalScopePolicy(apiSchemes);

builder.AddLupiraJwt(o =>
{
    o.Validation = JwtValidationProfile.Strict;
    o.RelaxHttpsMetadataInDevelopment = false;
    o.RequireConfig = OidcConfigRequirement.Always;
    o.DevOrJwtDefault = true;
});
```

Defaults: bearer is the default scheme, `RequireHttpsMetadata = !IsDevelopment()`, `/mcp` challenge on, Authority + Audience required outside Development (never under `getdocument`), `X-Dev-User` scheme `Dev` in Development.
