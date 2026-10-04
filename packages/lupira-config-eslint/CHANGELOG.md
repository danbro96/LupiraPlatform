# Changelog

## 0.1.0

- `pure()`: production code may import only its own package plus `allowModules`; tests exempt; optional React hook and Compiler rules.
- `web()`: downward-only data → state → ui with a `config` leaf.
- `mobile()`: downward-only domain → data → sync → state → ui with `feedback`/`debug`/`config` leaves and the generated client as its own element.
