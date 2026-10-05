# Changelog

## 0.1.2

- Depends on `lupira-domain-maps` ^0.2.0.

## 0.1.1
- Ships the `lupira-sync-maplibre` bin.

## 0.1.0

- `maplibreSetup`: side-effect module setting the worker URL and the `pmtiles` protocol.
- `useMapTheme`, `mapStyle` (`loadMapStyle`, `fallbackStyle` over the cookie transport), `MiniMap` (default export).
- Bin `lupira-sync-maplibre [outDir]`: copies the maplibre-gl worker and its shared module into `public/maplibre/`.
