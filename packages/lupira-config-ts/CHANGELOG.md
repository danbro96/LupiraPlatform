# Changelog

## 0.1.0

- `tsconfig.base.json`: strict bundler-mode, no-emit base for apps and source-only packages.
- `tsconfig.library.json`: composite ESM build of `src/` to `dist/` with declarations and sourcemaps; `.ts` import specifiers emit as `.js`; tests stay out of the build.
- `./vitest`: `vitestConfig()` — node environment, colocated `src/**/*.test.ts`, overridable.
