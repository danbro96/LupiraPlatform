import { plural } from '@danbro96/lupira-domain-core/wording';

export function hotspotStats(h: { activeDays: number; eventCount: number; photoCount: number }): string {
  return [
    plural(h.activeDays, 'day'),
    h.eventCount > 0 ? plural(h.eventCount, 'event') : null,
    h.photoCount > 0 ? plural(h.photoCount, 'photo') : null,
  ].filter(Boolean).join(' · ');
}
