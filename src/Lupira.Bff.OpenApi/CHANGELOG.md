# Changelog

## 0.3.1
- `NamespaceCollisions`: a schema that references a namespaced schema is namespaced too; two schemas dedupe only when their text matches after `$ref` rewriting.

## 0.3.0
- `AddLupiraBffOpenApi` hands each configured upstream spec to the proxy's route guards (`Lupira.Bff.Proxy` 0.2.0 `RouteGuardOptions`).

## 0.2.0
- Breaking: removed `NullableRefNormalizer` and `UpstreamSpecRefresh`; Kiota 1.35.0 generates from the unmodified upstream specs.

## 0.1.0
- `UpstreamSpecMerger` over the JSON DOM: documented allowlist filter, BFF mount and `pathMap` (mapped route parameters dropped), per-operation `SecurityFor`, `RetagByCluster`, `NamespaceCollisions` (schema rename with `$ref` retarget, operationId aliasing; off, a collision throws), `Version` override or upstream `info.version`, `SortPaths`.
- `BffDocumentTransformer` (additive: C#-declared paths win), `AddLupiraBffOpenApi(...)`, `MergeResult` with `NotExposed` and `Renames`.
- `NullableRefNormalizer` and `UpstreamSpecRefresh.TryRun(args)` for `--normalize-specs`.
