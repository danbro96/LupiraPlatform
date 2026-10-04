export type LupiraApp = 'cal' | 'maps' | 'photos' | 'tasks';

export type AppHosts = Record<LupiraApp, string>;

export const APP_SCHEMES: AppHosts = {
  cal: 'lupiracalendar',
  maps: 'lupiramaps',
  photos: 'lupiraphotos',
  tasks: 'lupiratasks',
};

export interface MapsAt {
  lon: number;
  lat: number;
  layers?: readonly string[];
}

export interface DayRange {
  from: string;
  to: string;
}

export interface AppLinks {
  calItemUrl(itemId: string): string;
  calContactUrl(contactId: string): string;
  mapsAtUrl(at: MapsAt): string;
  mapsRangeUrl(range: DayRange & { layers?: readonly string[] }): string;
  mapsPlacesUrl(): string;
  photosEventUrl(eventId: string, photoId?: string): string;
  photosPhotoUrl(photoId: string): string;
  photosRangeUrl(range: DayRange): string;
  tasksItemUrl(listId: string, itemId: string): string;
}

const enc = encodeURIComponent;

// 'none' is a choice, not an absence: an empty list must not bring the default layers back.
const layersParam = (layers: readonly string[] | undefined) =>
  layers === undefined ? '' : `&layers=${layers.length ? layers.map(enc).join(',') : 'none'}`;

export function webLinks(hosts: AppHosts): AppLinks {
  const base = (app: LupiraApp) => `${hosts[app].replace(/\/+$/, '')}/`;
  return {
    calItemUrl: (itemId) => `${base('cal')}?item=${enc(itemId)}`,
    calContactUrl: (contactId) => `${base('cal')}contacts/${enc(contactId)}`,
    mapsAtUrl: ({ lon, lat, layers }) => `${base('maps')}?at=${lon},${lat}${layersParam(layers)}`,
    mapsRangeUrl: ({ from, to, layers }) => `${base('maps')}?from=${enc(from)}&to=${enc(to)}${layersParam(layers)}`,
    mapsPlacesUrl: () => `${base('maps')}places`,
    photosEventUrl: (eventId, photoId) =>
      `${base('photos')}?event=${enc(eventId)}${photoId ? `&photo=${enc(photoId)}` : ''}`,
    photosPhotoUrl: (photoId) => `${base('photos')}?photo=${enc(photoId)}`,
    photosRangeUrl: ({ from, to }) => `${base('photos')}?from=${enc(from)}&to=${enc(to)}`,
    tasksItemUrl: (listId) => `${base('tasks')}lists/${enc(listId)}`,
  };
}

// Path forms match each app's React Navigation linking config; ranges ride on the root screen's query.
export function appLinks(schemes: AppHosts = APP_SCHEMES): AppLinks {
  const base = (app: LupiraApp) => `${schemes[app]}://`;
  return {
    calItemUrl: (itemId) => `${base('cal')}item/${enc(itemId)}`,
    calContactUrl: (contactId) => `${base('cal')}contact/${enc(contactId)}`,
    mapsAtUrl: ({ lon, lat, layers }) => `${base('maps')}at/${lon},${lat}${layersParam(layers).replace('&', '?')}`,
    mapsRangeUrl: ({ from, to, layers }) => `${base('maps')}?from=${enc(from)}&to=${enc(to)}${layersParam(layers)}`,
    mapsPlacesUrl: () => `${base('maps')}places`,
    photosEventUrl: (eventId, photoId) =>
      `${base('photos')}event/${enc(eventId)}${photoId ? `?photo=${enc(photoId)}` : ''}`,
    photosPhotoUrl: (photoId) => `${base('photos')}photo/${enc(photoId)}`,
    photosRangeUrl: ({ from, to }) => `${base('photos')}?from=${enc(from)}&to=${enc(to)}`,
    tasksItemUrl: (listId, itemId) => `${base('tasks')}task/${enc(listId)}/${enc(itemId)}`,
  };
}
