# Lupira.Hosting.LanEdge

Answers 404 to edge-routed requests on LAN/WireGuard-only prefixes. A request counts as edge-routed when it carries `X-Forwarded-For` (any reverse proxy: Cloudflare Tunnel, DMZ Traefik) or `CF-Ray` / `CF-Connecting-IP`. Direct LAN, WireGuard and service-to-service calls carry none of these; don't put an HTTP proxy between a LAN caller and a listed prefix.

```csharp
app.UseLanOnlySurfaces("/mcp", "/internal", "/.well-known/oauth-protected-resource");
```
