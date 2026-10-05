import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'oidc',
  allowModules: [
    '@danbro96/lupira-http',
    'expo-auth-session',
    'expo-crypto',
    'expo-secure-store',
    'zustand',
  ],
});
