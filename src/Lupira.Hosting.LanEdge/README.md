# Lupira.Hosting.LanEdge

Answers 404 to Cloudflare-tunnelled requests (`CF-Ray` / `CF-Connecting-IP`) on LAN/WireGuard-only prefixes.

```csharp
app.UseLanOnlySurfaces("/mcp", "/internal", "/.well-known/oauth-protected-resource");
```
