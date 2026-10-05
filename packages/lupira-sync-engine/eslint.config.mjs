import { pure } from '@danbro96/lupira-config-eslint';

const EXPO_ONLY = [
  '@danbro96/lupira-http',
  '@danbro96/lupira-http/*',
  '@react-native-community/netinfo',
  'expo-background-task',
  'expo-task-manager',
  'react-native',
];

export default [
  ...pure({
    element: 'sync',
    allowModules: [
      '@danbro96/lupira-expo-sqlite',
      '@danbro96/lupira-sync-core',
      ...EXPO_ONLY,
    ],
  }),
  {
    // The kernel runs without React Native; only the expo triggers may reach for it.
    files: ['src/**/*.ts'],
    ignores: ['src/expo/**'],
    rules: { 'no-restricted-imports': ['error', { patterns: [{ group: EXPO_ONLY }] }] },
  },
];
