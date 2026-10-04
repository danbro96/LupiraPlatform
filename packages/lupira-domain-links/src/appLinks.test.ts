import { describe, expect, it } from 'vitest';
import { appLinks, webLinks } from './appLinks.ts';

describe('webLinks', () => {
  const web = webLinks({
    cal: 'https://cal.lupira.com',
    maps: 'https://maps.lupira.com',
    photos: 'https://photos.lupira.com',
    tasks: 'https://tasks.lupira.com',
  });

  it('opens a calendar item and a contact', () => {
    expect(web.calItemUrl('i1')).toBe('https://cal.lupira.com/?item=i1');
    expect(web.calContactUrl('c1')).toBe('https://cal.lupira.com/contacts/c1');
  });

  it('centres the map on a point, with or without layers', () => {
    expect(web.mapsAtUrl({ lon: 13.1, lat: 60.1 })).toBe('https://maps.lupira.com/?at=13.1,60.1');
    expect(web.mapsAtUrl({ lon: 13.1, lat: 60.1, layers: ['photos'] })).toBe('https://maps.lupira.com/?at=13.1,60.1&layers=photos');
  });

  it('shows a day range on the map, and an empty layer list as none', () => {
    expect(web.mapsRangeUrl({ from: '2026-10-04', to: '2026-10-04', layers: ['photos', 'events'] }))
      .toBe('https://maps.lupira.com/?from=2026-10-04&to=2026-10-04&layers=photos,events');
    expect(web.mapsRangeUrl({ from: '2026-10-01', to: '2026-10-04', layers: [] }))
      .toBe('https://maps.lupira.com/?from=2026-10-01&to=2026-10-04&layers=none');
  });

  it('opens photos by event, photo and range', () => {
    expect(web.photosEventUrl('e1')).toBe('https://photos.lupira.com/?event=e1');
    expect(web.photosEventUrl('e1', 'p1')).toBe('https://photos.lupira.com/?event=e1&photo=p1');
    expect(web.photosPhotoUrl('p1')).toBe('https://photos.lupira.com/?photo=p1');
    expect(web.photosRangeUrl({ from: '2026-10-04', to: '2026-10-05' })).toBe('https://photos.lupira.com/?from=2026-10-04&to=2026-10-05');
  });

  it('opens a task in its list', () => {
    expect(web.tasksItemUrl('l1', 't1')).toBe('https://tasks.lupira.com/lists/l1');
    expect(web.mapsPlacesUrl()).toBe('https://maps.lupira.com/places');
  });

  it('takes other hosts, trailing slash or not', () => {
    const dev = webLinks({ cal: 'http://localhost:5174/', maps: 'http://m', photos: 'http://p', tasks: 'http://t' });
    expect(dev.calItemUrl('i1')).toBe('http://localhost:5174/?item=i1');
    expect(dev.mapsAtUrl({ lon: 1, lat: 2 })).toBe('http://m/?at=1,2');
  });

  it('encodes ids', () => {
    expect(web.calItemUrl('a b&c')).toBe('https://cal.lupira.com/?item=a%20b%26c');
  });
});

describe('appLinks', () => {
  const app = appLinks();

  it('opens each app through its scheme', () => {
    expect(app.calItemUrl('i1')).toBe('lupiracalendar://?item=i1');
    expect(app.calContactUrl('c1')).toBe('lupiracalendar://contacts/c1');
    expect(app.mapsAtUrl({ lon: 13.1, lat: 60.1, layers: ['photos'] })).toBe('lupiramaps://?at=13.1,60.1&layers=photos');
    expect(app.photosEventUrl('e1', 'p1')).toBe('lupiraphotos://?event=e1&photo=p1');
    expect(app.photosRangeUrl({ from: '2026-10-04', to: '2026-10-04' })).toBe('lupiraphotos://?from=2026-10-04&to=2026-10-04');
  });

  it('opens a task by its deep-link path', () => {
    expect(app.tasksItemUrl('l1', 't1')).toBe('lupiratasks://task/l1/t1');
    expect(app.mapsPlacesUrl()).toBe('lupiramaps://places');
  });
});
