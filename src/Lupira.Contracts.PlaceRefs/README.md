# Lupira.Contracts.PlaceRefs

Wire contract of the `/internal/<resource>/place-references:check` seam that the geo orphan sweep calls on every service holding place ids: `CheckPlaceReferencesRequest` → `PlaceReferencesResponse` of `PlaceReferenceCountDto`. BCL only.

Zero-count ids are omitted. `DeletedCount` counts soft-deleted referrers, which block a prune but not an orphan listing; a service without such referrers reports `0`.
