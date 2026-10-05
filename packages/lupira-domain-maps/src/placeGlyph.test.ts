import { describe, expect, it } from 'vitest';
import { PLACE_GLYPHS, placeGlyph } from './placeGlyph.ts';

describe('placeGlyph', () => {
  it('maps a category to its glyph, folding University into School', () => {
    expect(placeGlyph('Hotel')).toBe('hotel');
    expect(placeGlyph('University')).toBe('school');
    expect(placeGlyph('BusStop')).toBe('busStop');
  });

  it('leaves Unknown, Other and absent categories as a plain pin', () => {
    expect([placeGlyph('Unknown'), placeGlyph('Other'), placeGlyph(null), placeGlyph(undefined), placeGlyph('Nope')])
      .toEqual([null, null, null, null, null]);
  });

  it('uses every declared glyph', () => {
    const used = new Set(['Home', 'Office', 'Restaurant', 'Cafe', 'Bar', 'Store', 'Grocery', 'School', 'Clinic', 'Hospital', 'Pharmacy',
      'Gym', 'Park', 'Airport', 'Station', 'BusStop', 'Hotel', 'Landmark', 'Government', 'Worship'].map(placeGlyph));
    expect([...used].sort()).toEqual([...PLACE_GLYPHS].sort());
  });
});
