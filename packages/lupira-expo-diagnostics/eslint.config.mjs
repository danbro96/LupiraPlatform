import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'diagnostics',
  react: true,
  allowModules: [
    '@danbro96/lupira-expo-paper',
    '@danbro96/lupira-tokens-core',
    '@react-navigation/native',
    '@sentry/react-native',
    'expo-constants',
    'expo-updates',
    'react',
    'react-native',
    'react-native-paper',
    'zustand',
  ],
});
