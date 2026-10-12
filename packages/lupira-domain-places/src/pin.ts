import type { GeoPoint } from './geo.ts';

/** A pin this close to a known place is taken to be that place. */
export const PIN_DEDUPE_M = 25;

export interface PinStart extends GeoPoint {
  zoom: number;
}

const SWEDEN: PinStart = { lat: 62.9, lon: 16.3, zoom: 4 };
const HOUSE_NUMBER = /^\d+\s?[A-Za-z]?$/;

/** "Kyrkogatan 5, Ljungby" from a reverse-geocode hit, whose display name leads with the house number when there's
 *  no venue name; null without a hit. */
export function pinNameSuggestion(reverse: { displayName: string; locality?: string | null } | null | undefined): string | null {
  if (!reverse) return null;
  const parts = reverse.displayName.split(',').map((p) => p.trim()).filter(Boolean);
  if (parts.length === 0) return reverse.locality || null;
  const head = parts.length > 1 && HOUSE_NUMBER.test(parts[0]) ? `${parts[1]} ${parts[0]}` : parts[0];
  const town = reverse.locality;
  return town && !head.toLowerCase().includes(town.toLowerCase()) ? `${head}, ${town}` : head;
}

/** Best map view to start pinning from: the typed area beats a rejected hit beats where the event probably is
 *  beats the country. */
export function pinStart({ origin, typedLocality, hits = [] }: {
  origin?: GeoPoint | null;
  typedLocality?: GeoPoint | null;
  hits?: readonly { latitude: number; longitude: number }[];
}): PinStart {
  if (typedLocality) return { ...typedLocality, zoom: 12 };
  const hit = hits[0];
  if (hit) return { lat: hit.latitude, lon: hit.longitude, zoom: 14 };
  if (origin) return { ...origin, zoom: 13 };
  return SWEDEN;
}
