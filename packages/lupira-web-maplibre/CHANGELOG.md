# Changelog

## 0.3.1

- `MiniMap` and `MapPinDialog` route a failed map setup to `reportError`.

## 0.3.0

- `MapPinDialog`: optional `initialPin` (marker placed on open, map centred on it at street zoom or closer) and `onPinChange(point | null)` (fired on open, on placing and after each drag).
- `pinView`: the dialog's opening camera from `start` and an optional pin.

## 0.2.0

- `MapPinDialog` (default export): MUI dialog with a basemap where a click drops a draggable marker. Props `open`, `start` `{ lat, lon, zoom? }`, `geoBase`, `onCancel`, `onConfirm(point)`, optional `title`, `children` (shown above the map) and `confirmDisabled`.
- Depends on `lupira-domain-places` ^0.2.0.

## 0.1.2

- Depends on `lupira-domain-maps` ^0.2.0.

## 0.1.1
- Ships the `lupira-sync-maplibre` bin.

## 0.1.0

- `maplibreSetup`: side-effect module setting the worker URL and the `pmtiles` protocol.
- `useMapTheme`, `mapStyle` (`loadMapStyle`, `fallbackStyle` over the cookie transport), `MiniMap` (default export).
- Bin `lupira-sync-maplibre [outDir]`: copies the maplibre-gl worker and its shared module into `public/maplibre/`.
