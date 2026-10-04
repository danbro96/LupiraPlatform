# Changelog

## 0.1.0
- `YarpDependencyTargetSource`: one anonymous `readyz` target per `ReverseProxy:Clusters:*` entry (first non-blank `Destinations:*:Address`), named via a configurable cluster → registry-name map; an unmapped cluster fails fast.
- `AddLupiraDepzYarpTargets(Action<YarpDependencyTargetOptions>)`.
