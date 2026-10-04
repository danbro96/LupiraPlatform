import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'oidc',
  allowModules: [
    'expo-auth-session',
    'expo-crypto',
    'expo-secure-store',
  ],
});
