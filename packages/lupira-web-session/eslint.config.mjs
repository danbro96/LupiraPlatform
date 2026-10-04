import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'session',
  react: true,
  allowModules: [
    '@danbro96/lupira-http',
    '@tanstack/react-query',
    'react',
    'react-router',
  ],
});
