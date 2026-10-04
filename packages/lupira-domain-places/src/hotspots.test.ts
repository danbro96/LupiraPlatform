import { describe, expect, it } from 'vitest';
import { hotspotStats } from './hotspots.ts';

describe('hotspotStats', () => {
  it('reads days, then events and photos only when there are any', () => {
    expect(hotspotStats({ activeDays: 1, eventCount: 1, photoCount: 0 })).toBe('1 day · 1 event');
    expect(hotspotStats({ activeDays: 12, eventCount: 0, photoCount: 80 })).toBe('12 days · 80 photos');
  });
});
