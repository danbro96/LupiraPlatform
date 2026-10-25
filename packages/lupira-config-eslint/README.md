# @danbro96/lupira-config-eslint

```js
// eslint.config.mjs
import { mobile } from '@danbro96/lupira-config-eslint';
export default mobile({ tsconfigRootDir: import.meta.dirname });
```

Presets: `pure` (tokens/domain packages), `web`, `mobile`. Built on eslint-plugin-boundaries v7; React apps also get `react-hooks` `recommended-latest`. Type-aware (`projectService`): un-awaited promises (`no-floating-promises`, `void` included) and promise-returning callbacks where `void` is expected (`no-misused-promises`) are errors; `invalidateQueries` and the sync engine's `sync()` never reject and may float when written bare. Linted files must belong to a tsconfig. `pure()` lints test files untyped.

Options: `mobile({ ignores, generated, domainImportsGenerated, layers })`, `web({ ignores, domain })`, `pure({ element, allowModules, react })`; every preset takes `tsconfigRootDir` (default: the cwd). `layers` takes `{ name, pattern, imports, importedBy }` for folders outside the stack, e.g. a headless collector:

```js
export default mobile({ layers: [{ name: 'collector', pattern: 'src/collector/**', imports: ['data', 'domain'], importedBy: ['state', 'ui'] }] });
```
