# Changelog

## 0.1.0
- `AddLupiraDefaults(configure?)`: enums as names and strict numbers on the HTTP JSON contract; opt-in case-insensitive properties, UTC `DateTimeOffset` output and `ThrowOnBadRequest`; data-protection keys persisted to `DataProtection:KeyPath` under the host's application name.
- `UseLupiraDefaults()`: `X-Forwarded-For`/`-Proto` (configurable) trusted from any proxy; ProblemDetails status-code pages outside `/mcp` (configurable).
