import type { GeoPoint } from '@danbro96/lupira-domain-places/geo';

export const DEFAULT_PIN_ZOOM = 13;

export interface PinView {
  center: [lon: number, lat: number];
  zoom: number;
}

/** An existing pin is centred and shown at street level at least, so a country-wide `start` can't hide it. */
export function pinView(start: { lat: number; lon: number; zoom?: number }, initialPin?: GeoPoint | null): PinView {
  const zoom = start.zoom ?? DEFAULT_PIN_ZOOM;
  if (!initialPin) return { center: [start.lon, start.lat], zoom };
  return { center: [initialPin.lon, initialPin.lat], zoom: Math.max(zoom, DEFAULT_PIN_ZOOM) };
}
