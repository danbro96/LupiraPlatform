import { fallbackStyle as sharedFallback, loadBasemapStyle } from '@danbro96/lupira-domain-maps/mapStyle';
import type { MapTheme } from '@danbro96/lupira-tokens-map/map';
import type { StyleSpecification } from 'maplibre-gl';

/** The document logic is shared with the mobile app; only the cookie transport is the web's. */
const sameOrigin = (url: string, headers?: Record<string, string>) => fetch(url, { headers, credentials: 'include' });

// BasemapStyle is a slice of the spec, so the widening needs the detour through unknown.
export async function loadMapStyle(theme: MapTheme, geoBase: string): Promise<StyleSpecification> {
  const style = await loadBasemapStyle(sameOrigin, `${geoBase}/basemap/style.json?theme=${theme}`, window.location.origin);
  return style as unknown as StyleSpecification;
}

export function fallbackStyle(theme: MapTheme, geoBase: string): StyleSpecification {
  return sharedFallback(theme, `${window.location.origin}${geoBase}`) as unknown as StyleSpecification;
}
