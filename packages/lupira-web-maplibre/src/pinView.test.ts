import { describe, expect, it } from 'vitest';
import { DEFAULT_PIN_ZOOM, pinView } from './pinView.ts';

describe('pinView', () => {
  it('uses start without a pin', () => {
    expect(pinView({ lat: 62.9, lon: 16.3, zoom: 4 })).toEqual({ center: [16.3, 62.9], zoom: 4 });
    expect(pinView({ lat: 56.8, lon: 13.9 }, null)).toEqual({ center: [13.9, 56.8], zoom: DEFAULT_PIN_ZOOM });
  });

  it('centres on the pin at street level at least', () => {
    expect(pinView({ lat: 62.9, lon: 16.3, zoom: 4 }, { lat: 60.1, lon: 13.0 })).toEqual({ center: [13.0, 60.1], zoom: DEFAULT_PIN_ZOOM });
  });

  it('keeps a closer start zoom with a pin', () => {
    expect(pinView({ lat: 60.1, lon: 13.0, zoom: 17 }, { lat: 60.1, lon: 13.0 })).toEqual({ center: [13.0, 60.1], zoom: 17 });
  });
});
