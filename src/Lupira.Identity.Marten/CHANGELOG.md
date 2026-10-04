# Changelog

## 0.1.0
- `Principal` and `PrincipalDirectory` (with `FindBySubAsync` and `LookupAsync`), unified from the Cal, Career, Contact, Geo, Health, Location, Photo and Tasks copies; generic `PrincipalDirectory<TPrincipal>` for a service-specific principal subclass.
- `EventActor`: `Of`, the principal `Stamp` and the command `Stamp`.
- `AddLupiraPrincipals()` (StoreOptions) and `AddLupiraPrincipalDirectory()` (services).
