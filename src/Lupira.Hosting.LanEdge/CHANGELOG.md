# Changelog

## 1.1.0
- `X-Forwarded-For` also marks a request as edge-routed, so prefixes stay 404 behind a non-Cloudflare reverse proxy (DMZ Traefik). A LAN-side HTTP proxy in front of a LAN-only prefix now gets 404 too.

## 1.0.0
- `UseLanOnlySurfaces(params string[] lanOnlyPrefixes)`: requests carrying `CF-Ray` / `CF-Connecting-IP` on a listed prefix get 404.
