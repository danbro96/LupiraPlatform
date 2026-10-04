import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'maplibre',
  react: true,
  allowModules: [
    '@danbro96/lupira-domain-maps',
    '@danbro96/lupira-tokens-map',
    '@mui/material',
    'maplibre-gl',
    'pmtiles',
    'react',
  ],
});
