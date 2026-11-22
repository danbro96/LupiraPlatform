# Changelog

## 1.2.0
- SA1501 and SA1623 off: braceless one-line statements are the house style (SA1503 already off), and "Gets or sets" summaries would replace the OpenAPI property descriptions.
- SA1313 off: it rejects `_` for an unused parameter, the name IDE0060 asks for (e.g. a Marten `Apply(Event _)`).

## 1.1.0
- XML docs of referenced `Lupira.*` assemblies reach the OpenAPI source generator, so package types keep their descriptions in a consumer's document.

## 1.0.0
- Language defaults (net10.0, nullable, implicit usings, code style in build), StyleCop.Analyzers, analyzer severities as a global config (SA1402/SA1649 = error).
