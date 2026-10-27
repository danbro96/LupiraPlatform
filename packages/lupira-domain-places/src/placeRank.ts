// One ranked list for the event place picker. Every source a place is known from (saved, frequent, typeahead,
// a contact's address) folds into one candidate, scored as text match × personal affinity + event context.
// Past visits decay, so a haunt from years ago stops outranking last month's; a selected attendee's home leads.

import { haversineM, type GeoPoint } from './geo.ts';
import type { ResidencyStatus } from '@danbro96/lupira-domain-contacts/fuzzyDate';
import { searchTerms } from '@danbro96/lupira-domain-core/textSearch';

export interface PlaceCandidate {
  placeId: string;
  label: string;
  context?: string | null;
  saved?: boolean;
  hotspot?: { activeDays: number; lastDay: string };
  /** Position in the server's typeahead answer, 0 = best. */
  suggestRank?: number;
  /** Reached because the query matched this contact's name. */
  viaContact?: { contactId: string; status: ResidencyStatus };
  residents?: { contactId: string; status: ResidencyStatus }[];
  point?: GeoPoint | null;
  /** Your own events here; the category pair counts only events of the kind being placed. */
  usage?: { count: number; lastDay: string; categoryCount?: number; categoryLastDay?: string };
  placeCategory?: string;
  /** Who you've been here with, by shared events. */
  companions?: { contactId: string; count: number }[];
  isHome?: boolean;
}

export interface PlaceRankContext {
  query: string;
  now: Date;
  attendeeIds: ReadonlySet<string>;
  /** Participation score per contact, any scale; normalized against the largest. */
  contactScores?: ReadonlyMap<string, number>;
  /** Where the event probably is — the day's other events, or you. */
  origin?: GeoPoint | null;
  /** The app's policy for how well a place category suits the event being placed; weights category usage. */
  categoryAffinity?: (placeCategory: string | undefined, isHome: boolean) => number;
  /** 0..1 — how far past use is scored by recency alone rather than frequency. */
  recencyBias?: number;
  /** Scales a place's all-kinds history (hotspot, overall usage); default 1. */
  historyWeight?: (c: PlaceCandidate) => number;
}

const DAY_MS = 86_400_000;
const HOTSPOT_HALF_LIFE_DAYS = 180;
const HOTSPOT_FULL_DAYS = 30;
const HOTSPOT_MAX = 1.5;
const COMPANION_FULL = 10;
const ATTENDEE_HOME_BOOST = 3;
const PROXIMITY_WEIGHT = 0.3;
/** A former/future address found by a contact's name: still findable, but below any real match. */
const OTHER_RESIDENCY_MATCH = 0.3;

/** Folds duplicates (same placeId from several sources) into one candidate. */
export function mergeCandidates(list: readonly PlaceCandidate[]): PlaceCandidate[] {
  const byId = new Map<string, PlaceCandidate>();
  for (const c of list) {
    const prev = byId.get(c.placeId);
    if (!prev) {
      byId.set(c.placeId, { ...c, residents: c.residents ? [...c.residents] : undefined });
      continue;
    }
    const residents = [...(prev.residents ?? [])];
    for (const r of c.residents ?? []) if (!residents.some((x) => x.contactId === r.contactId)) residents.push(r);
    byId.set(c.placeId, {
      ...prev,
      context: prev.context ?? c.context,
      saved: prev.saved || c.saved,
      hotspot: prev.hotspot ?? c.hotspot,
      suggestRank: minDefined(prev.suggestRank, c.suggestRank),
      viaContact: bestVia(prev.viaContact, c.viaContact),
      residents: residents.length ? residents : undefined,
      point: prev.point ?? c.point,
      usage: prev.usage ?? c.usage,
      placeCategory: prev.placeCategory ?? c.placeCategory,
      companions: prev.companions ?? c.companions,
      isHome: prev.isHome || c.isHome,
    });
  }
  return [...byId.values()];
}

export function textMatch(c: PlaceCandidate, query: string): number {
  const terms = searchTerms(query);
  if (terms.length === 0) return 1;
  let best = 0;
  const words = c.label.toLowerCase().split(/[\s,.-]+/).filter(Boolean);
  const label = c.label.toLowerCase();
  if (terms.every((t) => words.some((w) => w.startsWith(t)))) best = 1;
  else if (terms.every((t) => label.includes(t))) best = 0.6;
  // The server matched aliases and localities the label doesn't show.
  if (c.suggestRank != null) best = Math.max(best, 0.9 * 0.93 ** c.suggestRank);
  if (c.viaContact) best = Math.max(best, c.viaContact.status === 'active' ? 1 : OTHER_RESIDENCY_MATCH);
  return best;
}

export function affinity(c: PlaceCandidate, ctx: PlaceRankContext): number {
  let a = c.saved ? 1 : 0;
  const active = (c.residents ?? []).filter((r) => r.status === 'active');
  if (active.length > 0) {
    const top = Math.max(0, ...(ctx.contactScores?.values() ?? []));
    const scoreOf = (id: string) => (top > 0 ? (ctx.contactScores?.get(id) ?? 0) / top : 0);
    a += 0.5 + Math.max(...active.map((r) => scoreOf(r.contactId)));
  }
  const history = ctx.historyWeight?.(c) ?? 1;
  if (c.hotspot) a += history * decayedUse(c.hotspot.activeDays, c.hotspot.lastDay, ctx);
  if (c.usage) {
    a += history * decayedUse(c.usage.count, c.usage.lastDay, ctx);
    if (c.usage.categoryCount) {
      const weight = ctx.categoryAffinity?.(c.placeCategory, c.isHome ?? false) ?? 1;
      a += weight * decayedUse(c.usage.categoryCount, c.usage.categoryLastDay ?? c.usage.lastDay, ctx);
    }
  }
  const together = (c.companions ?? []).filter((x) => ctx.attendeeIds.has(x.contactId));
  if (together.length > 0) a += Math.min(1, together.reduce((n, x) => n + Math.log1p(x.count), 0) / Math.log1p(COMPANION_FULL));
  return a;
}

function decayedUse(count: number, lastDay: string, ctx: PlaceRankContext): number {
  const ageDays = Math.max(0, (ctx.now.getTime() - Date.parse(lastDay)) / DAY_MS) || 0;
  const frequency = Math.min(HOTSPOT_MAX, Math.log1p(count) / Math.log1p(HOTSPOT_FULL_DAYS));
  const bias = Math.min(1, Math.max(0, ctx.recencyBias ?? 0));
  return ((1 - bias) * frequency + bias) * 0.5 ** (ageDays / HOTSPOT_HALF_LIFE_DAYS);
}

export function contextBoost(c: PlaceCandidate, ctx: PlaceRankContext): number {
  let b = 0;
  if ((c.residents ?? []).some((r) => r.status === 'active' && ctx.attendeeIds.has(r.contactId))) b += ATTENDEE_HOME_BOOST;
  if (ctx.origin && c.point) b += PROXIMITY_WEIGHT / (1 + haversineM(ctx.origin, c.point) / 1000);
  return b;
}

export function placeScore(c: PlaceCandidate, ctx: PlaceRankContext): number {
  const match = textMatch(c, ctx.query);
  if (match === 0) return 0;
  return match * (1 + affinity(c, ctx)) + contextBoost(c, ctx);
}

/** Merged, scored, best first; with a query, candidates that don't match it drop out. */
export function rankPlaces(list: readonly PlaceCandidate[], ctx: PlaceRankContext): PlaceCandidate[] {
  return mergeCandidates(list)
    .map((c) => ({ c, score: placeScore(c, ctx) }))
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score || a.c.label.localeCompare(b.c.label))
    .map((x) => x.c);
}

function minDefined(a: number | undefined, b: number | undefined): number | undefined {
  if (a == null) return b;
  if (b == null) return a;
  return Math.min(a, b);
}

function bestVia(a: PlaceCandidate['viaContact'], b: PlaceCandidate['viaContact']): PlaceCandidate['viaContact'] {
  if (!a) return b;
  if (!b) return a;
  return b.status === 'active' && a.status !== 'active' ? b : a;
}
