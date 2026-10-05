export const PLACE_GLYPHS = [
  'home', 'office', 'restaurant', 'cafe', 'bar', 'store', 'grocery', 'school', 'clinic', 'hospital',
  'pharmacy', 'gym', 'park', 'airport', 'station', 'busStop', 'hotel', 'landmark', 'government', 'worship',
] as const;

export type PlaceGlyph = (typeof PLACE_GLYPHS)[number];

/** geo-api `PlaceCategory` names, taken as strings so this stays free of the generated models. */
const GLYPH_BY_CATEGORY: Record<string, PlaceGlyph> = {
  Home: 'home',
  Office: 'office',
  Restaurant: 'restaurant',
  Cafe: 'cafe',
  Bar: 'bar',
  Store: 'store',
  Grocery: 'grocery',
  School: 'school',
  University: 'school',
  Clinic: 'clinic',
  Hospital: 'hospital',
  Pharmacy: 'pharmacy',
  Gym: 'gym',
  Park: 'park',
  Airport: 'airport',
  Station: 'station',
  BusStop: 'busStop',
  Hotel: 'hotel',
  Landmark: 'landmark',
  Government: 'government',
  Worship: 'worship',
};

/** The glyph a place category is drawn with; null for `Unknown`, `Other` and absent, which stay a plain pin. */
export function placeGlyph(category: string | null | undefined): PlaceGlyph | null {
  return GLYPH_BY_CATEGORY[category ?? ''] ?? null;
}
