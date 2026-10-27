import { describe, expect, it } from 'vitest';
import { affinity, mergeCandidates, placeScore, rankPlaces, type PlaceCandidate, type PlaceRankContext } from './placeRank.ts';

const now = new Date('2026-09-30T12:00:00Z');
const ctx = (over: Partial<PlaceRankContext> = {}): PlaceRankContext => ({ query: '', now, attendeeIds: new Set(), ...over });
const ids = (list: PlaceCandidate[]) => list.map((c) => c.placeId);

describe('rankPlaces', () => {
  it('ranks a recent haunt above one abandoned years ago', () => {
    const ranked = rankPlaces([
      { placeId: 'old-gym', label: 'Gym', hotspot: { activeDays: 120, lastDay: '2023-01-10' } },
      { placeId: 'cafe', label: 'Café', hotspot: { activeDays: 8, lastDay: '2026-09-20' } },
    ], ctx());
    expect(ids(ranked)).toEqual(['cafe', 'old-gym']);
  });

  it("leads with a selected attendee's home", () => {
    const ranked = rankPlaces([
      { placeId: 'saved', label: 'Office', saved: true, hotspot: { activeDays: 200, lastDay: '2026-09-29' } },
      { placeId: 'annas', label: 'Storgatan 1', residents: [{ contactId: 'anna', status: 'active' }] },
    ], ctx({ attendeeIds: new Set(['anna']) }));
    expect(ids(ranked)).toEqual(['annas', 'saved']);
  });

  it('drops what the query does not match, and finds a place by its resident', () => {
    const ranked = rankPlaces([
      { placeId: 'office', label: 'Office', saved: true },
      { placeId: 'annas', label: 'Storgatan 1', viaContact: { contactId: 'anna', status: 'active' }, residents: [{ contactId: 'anna', status: 'active' }] },
    ], ctx({ query: 'ann' }));
    expect(ids(ranked)).toEqual(['annas']);
  });

  it('keeps a former address findable but below a current one', () => {
    const ranked = rankPlaces([
      { placeId: 'then', label: 'Old flat', viaContact: { contactId: 'anna', status: 'former' } },
      { placeId: 'suggested', label: 'Annas Bageri', suggestRank: 3 },
      { placeId: 'now', label: 'Home', viaContact: { contactId: 'anna', status: 'active' }, residents: [{ contactId: 'anna', status: 'active' }] },
    ], ctx({ query: 'anna' }));
    expect(ids(ranked)).toEqual(['now', 'suggested', 'then']);
  });

  it('breaks ties by distance to the event', () => {
    const ranked = rankPlaces([
      { placeId: 'far', label: 'Pizza Göteborg', point: { lat: 57.7, lon: 11.97 } },
      { placeId: 'near', label: 'Pizza Söder', point: { lat: 59.31, lon: 18.07 } },
    ], ctx({ query: 'pizza', origin: { lat: 59.33, lon: 18.07 } }));
    expect(ids(ranked)).toEqual(['near', 'far']);
  });

  it('weighs a resident by how much you meet them', () => {
    const ranked = rankPlaces([
      { placeId: 'bos', label: 'B', residents: [{ contactId: 'bo', status: 'active' }] },
      { placeId: 'annas', label: 'A', residents: [{ contactId: 'anna', status: 'active' }] },
    ], ctx({ contactScores: new Map([['anna', 5], ['bo', 0.5]]) }));
    expect(ids(ranked)).toEqual(['annas', 'bos']);
  });
});

describe('usage, category and companions', () => {
  const legacy: PlaceCandidate[] = [
    { placeId: 'gym', label: 'Gym', hotspot: { activeDays: 120, lastDay: '2023-01-10' } },
    { placeId: 'cafe', label: 'Café', saved: true, hotspot: { activeDays: 8, lastDay: '2026-09-20' }, point: { lat: 59.3, lon: 18 } },
    { placeId: 'annas', label: 'Storgatan 1', residents: [{ contactId: 'anna', status: 'active' }] },
  ];
  const legacyCtx = ctx({ attendeeIds: new Set(['anna']), contactScores: new Map([['anna', 2]]), origin: { lat: 59.33, lon: 18.07 } });

  it('scores candidates without the new fields exactly as before', () => {
    const scores = legacy.map((c) => placeScore(c, legacyCtx));
    expect(scores[0]).toBeCloseTo(1.0074378749575421, 12);
    expect(scores[1]).toBeCloseTo(2.662979440530713, 12);
    expect(scores[2]).toBeCloseTo(5.5, 12);
    const withPolicy = { ...legacyCtx, categoryAffinity: () => 5 };
    expect(legacy.map((c) => placeScore(c, withPolicy))).toEqual(scores);
  });

  it('ranks your often-used place above a one-off, and decays old use', () => {
    const ranked = rankPlaces([
      { placeId: 'once', label: 'A', usage: { count: 1, lastDay: '2026-09-20' } },
      { placeId: 'often', label: 'B', usage: { count: 25, lastDay: '2026-09-01' } },
      { placeId: 'stale', label: 'C', usage: { count: 25, lastDay: '2021-09-01' } },
    ], ctx());
    expect(ids(ranked)).toEqual(['often', 'once', 'stale']);
  });

  it('with a recency bias, prefers the place used last over the most frequent', () => {
    const list: PlaceCandidate[] = [
      { placeId: 'often', label: 'A', usage: { count: 25, lastDay: '2026-06-01' } },
      { placeId: 'last', label: 'B', usage: { count: 1, lastDay: '2026-09-25' } },
    ];
    expect(ids(rankPlaces(list, ctx()))).toEqual(['often', 'last']);
    expect(ids(rankPlaces(list, ctx({ recencyBias: 1 })))).toEqual(['last', 'often']);
  });

  it('weighs use for this kind of event by the category policy', () => {
    const list: PlaceCandidate[] = [
      { placeId: 'home', label: 'A', isHome: true, usage: { count: 5, lastDay: '2026-09-20', categoryCount: 5, categoryLastDay: '2026-09-20' } },
      { placeId: 'clinic', label: 'B', placeCategory: 'Healthcare', usage: { count: 3, lastDay: '2026-09-20', categoryCount: 3 } },
    ];
    expect(ids(rankPlaces(list, ctx()))).toEqual(['home', 'clinic']);
    const appointment = (category: string | undefined, isHome: boolean) => (isHome ? 0 : category === 'Healthcare' ? 2 : 1);
    expect(ids(rankPlaces(list, ctx({ categoryAffinity: appointment })))).toEqual(['clinic', 'home']);
  });

  it('scales hotspot and overall usage by the history weight, not same-kind usage', () => {
    const c: PlaceCandidate = {
      placeId: 'p', label: 'A', hotspot: { activeDays: 18, lastDay: '2026-09-01' },
      usage: { count: 10, lastDay: '2026-09-01', categoryCount: 4, categoryLastDay: '2026-09-01' },
    };
    const sameKind = affinity({ ...c, hotspot: undefined, usage: { ...c.usage!, count: 0 } }, ctx());
    const full = affinity(c, ctx());
    expect(affinity(c, ctx({ historyWeight: () => 1 }))).toBe(full);
    expect(affinity(c, ctx({ historyWeight: () => 0 }))).toBeCloseTo(sameKind);
    expect(affinity(c, ctx({ historyWeight: () => 0.5 }))).toBeCloseTo((full + sameKind) / 2);
  });

  it('favours where you have been with the invited people', () => {
    const list: PlaceCandidate[] = [
      { placeId: 'pub', label: 'A', companions: [{ contactId: 'bo', count: 12 }] },
      { placeId: 'cafe', label: 'B', companions: [{ contactId: 'anna', count: 2 }] },
    ];
    expect(ids(rankPlaces(list, ctx({ attendeeIds: new Set(['anna']) })))).toEqual(['cafe', 'pub']);
    expect(affinity(list[0], ctx({ attendeeIds: new Set(['bo']) }))).toBe(1);
  });
});

describe('mergeCandidates', () => {
  it('folds every source of one place into one candidate', () => {
    const [merged, ...rest] = mergeCandidates([
      { placeId: 'p', label: 'Home', saved: true },
      { placeId: 'p', label: 'Home', suggestRank: 2, residents: [{ contactId: 'anna', status: 'active' }] },
      { placeId: 'p', label: 'Home', suggestRank: 1, residents: [{ contactId: 'anna', status: 'active' }] },
    ]);
    expect(rest).toEqual([]);
    expect(merged).toMatchObject({ saved: true, suggestRank: 1, residents: [{ contactId: 'anna', status: 'active' }] });
  });

  it('keeps usage, category and companions from whichever source has them', () => {
    const [merged] = mergeCandidates([
      { placeId: 'p', label: 'Clinic', suggestRank: 0 },
      { placeId: 'p', label: 'Clinic', usage: { count: 2, lastDay: '2026-09-01' }, placeCategory: 'Healthcare', companions: [{ contactId: 'anna', count: 1 }], isHome: false },
    ]);
    expect(merged).toMatchObject({ usage: { count: 2 }, placeCategory: 'Healthcare', companions: [{ contactId: 'anna', count: 1 }], isHome: false });
  });
});
