import { describe, expect, it } from 'vitest';
import { pinNameSuggestion, pinStart } from './pin.ts';

describe('pinNameSuggestion', () => {
  it('puts the house number after the street and adds the locality', () => {
    expect(pinNameSuggestion({ displayName: '5, Kyrkogatan, Centrum, Ljungby, Sverige', locality: 'Ljungby' })).toBe('Kyrkogatan 5, Ljungby');
    expect(pinNameSuggestion({ displayName: '12B, Storgatan, Torsby', locality: 'Torsby' })).toBe('Storgatan 12B, Torsby');
  });

  it('keeps a venue name and skips a locality it already contains', () => {
    expect(pinNameSuggestion({ displayName: 'Café Saturnus, Eriksbergsgatan, Stockholm', locality: 'Stockholm' })).toBe('Café Saturnus, Stockholm');
    expect(pinNameSuggestion({ displayName: 'Torsby station, Järnvägsgatan', locality: 'Torsby' })).toBe('Torsby station');
    expect(pinNameSuggestion({ displayName: 'Kyrkogatan' })).toBe('Kyrkogatan');
  });

  it('falls back to the locality, and is null without a hit', () => {
    expect(pinNameSuggestion({ displayName: ' , ', locality: 'Ljungby' })).toBe('Ljungby');
    expect(pinNameSuggestion({ displayName: '' })).toBeNull();
    expect(pinNameSuggestion(null)).toBeNull();
  });
});

describe('pinStart', () => {
  const origin = { lat: 60.13, lon: 12.99 };
  const hits = [{ latitude: 59.33, longitude: 18.07 }, { latitude: 57.7, longitude: 11.97 }];

  it('prefers the typed area, then a rejected hit, then the event origin', () => {
    expect(pinStart({ origin, typedLocality: { lat: 56.88, lon: 14.81 }, hits })).toEqual({ lat: 56.88, lon: 14.81, zoom: 12 });
    expect(pinStart({ origin, hits })).toEqual({ lat: 59.33, lon: 18.07, zoom: 14 });
    expect(pinStart({ origin, typedLocality: null, hits: [] })).toEqual({ ...origin, zoom: 13 });
  });

  it('shows the whole country with nothing to go on', () => {
    expect(pinStart({})).toEqual({ lat: 62.9, lon: 16.3, zoom: 4 });
  });
});
