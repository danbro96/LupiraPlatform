# Changelog

## 0.2.0
- `appLinks` builds the path forms the apps route: `lupiracalendar://item/{id}` and `contact/{id}`, `lupiramaps://at/{lon},{lat}`, `lupiraphotos://event/{id}` and `photo/{id}`; ranges stay on the root query.

## 0.1.0

- `appLinks`: `webLinks(hosts)` and `appLinks(schemes = APP_SCHEMES)` build `calItemUrl`, `calContactUrl`, `mapsAtUrl`, `mapsRangeUrl`, `mapsPlacesUrl`, `photosEventUrl`, `photosPhotoUrl`, `photosRangeUrl`, `tasksItemUrl`.
