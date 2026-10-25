import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'oidc',
  react: true,
  allowModules: [
    '@danbro96/lupira-http',
    'expo-auth-session',
    'expo-crypto',
    'expo-secure-store',
    'react',
    'react-native',
    'zustand',
  ],
});
