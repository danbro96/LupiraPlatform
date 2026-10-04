# Changelog

## 0.1.0
- `OidcAuthOptions` (`Auth:Oidc` Authority/Audience, `IsConfigured`), unified from the API copies.
- `AddLupiraJwt(configure?)` on `IHostApplicationBuilder`: bearer from `SectionName`; `Validation` (`Default`, `RawClaims`, `Strict`), `RelaxHttpsMetadataInDevelopment`, `McpChallenge` (RFC 9728 `resource_metadata` 401 on `/mcp`), `RequireConfig` (`None`, `OutsideDevelopment`, `Always`; skipped under `getdocument`), `SkipBearerWhenUnconfigured`, Development `DevScheme` / `RegisterDevScheme`, `DevOrJwtDefault`. Returns the `AuthenticationBuilder`.
- `LupiraJwtSchemes.Api(environment)`; `AddLupiraApiPolicy`, `AddLupiraInternalScopePolicy` (`internal:read`), `AddLupiraGatewayAzpPolicy` on `AuthorizationBuilder`.
