# @danbro96/lupira-config-eslint

```js
// eslint.config.mjs
import { mobile } from '@danbro96/lupira-config-eslint';
export default mobile();
```

Presets: `pure` (tokens/domain packages), `web`, `mobile`. Built on eslint-plugin-boundaries v7; React apps also get `react-hooks` `recommended-latest`.

Options: `mobile({ ignores, generated, domainImportsGenerated, layers })`, `web({ ignores, domain })`. `layers` takes `{ name, pattern, imports, importedBy }` for folders outside the stack, e.g. a headless collector:

```js
export default mobile({ layers: [{ name: 'collector', pattern: 'src/collector/**', imports: ['data', 'domain'], importedBy: ['state', 'ui'] }] });
```
