# @danbro96/lupira-config-eslint

```js
// eslint.config.mjs
import { mobile } from '@danbro96/lupira-config-eslint';
export default mobile();
```

Presets: `pure` (tokens/domain packages), `web`, `mobile`. Built on eslint-plugin-boundaries v7; React apps also get `react-hooks` `recommended-latest`.
