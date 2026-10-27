# Changelog

## 0.3.0

- `placeRank`: `PlaceRankContext` takes optional `historyWeight(candidate)`, scaling a place's hotspot and overall-usage terms in `affinity`; same-kind usage is unaffected. Omitted, scores are unchanged.

## 0.2.1

- `pickPlaces` returns only `places`; the unused `residentsNear` lookup is gone.

## 0.2.0

- **Breaking:** `placeRequestFromHit(hit, name?)` returns `{ kind: 'fromGeocode', osmType, osmId, name? }` or `{ kind: 'fromPin', name?, lat, lon }` (`PlaceRequest`). The `query` field, the `categories` parameter and the plain `create` request are gone; a hit without OSM ids becomes a pin. Callers in LupiraCal web (`usePlaces`) and mobile (`usePlaceSearch`) move to the from-geocode/from-pin endpoints.
- `placeRank`: `PlaceCandidate` takes optional `usage`, `placeCategory`, `companions` and `isHome`; `PlaceRankContext` takes optional `categoryAffinity` and `recencyBias`. `affinity` scores decayed usage, category usage weighted by `categoryAffinity`, and companions among the attendees. Candidates without these fields score as before; `mergeCandidates` keeps the new fields.
- New `pin` module: `pinNameSuggestion`, `pinStart`, `PinStart`, `PIN_DEDUPE_M`.
- Depends on `lupira-domain-contacts` ^0.2.0.

## 0.1.0

- `geo`: `haversineM`, `bboxOf`, `padBbox`, `mapViewport`, `bboxToParam`, `splitTrack`, `durationMin`, `trackWindowFrozen`, `movementStaleMs`, `LIVE_FIX_POLL_MS`, `LIVE_FIX_STALE_MS`.
- `places`: `osmUrl`, `formatCoords`, `distinctPlaceIds`, `chunk`, `toLocatedPlaces`, `placeRequestFromHit`, `placeTitle`, `PLACE_LOOKUP_MAX`, `GEOCODER_UNAVAILABLE`.
- `placeRank`: `rankPlaces`, `mergeCandidates`, `placeScore`, `textMatch`, `affinity`, `contextBoost`.
- `placeCandidates`: `pickPlaces`, `eventOrigin`, `MIN_PLACE_QUERY`, `PLACE_SEARCH_DEBOUNCE_MS`, `PLACE_SUGGEST_LIMIT`, `ADDRESS_SEARCH_LIMIT`.
- `hotspots`: `hotspotStats`.
