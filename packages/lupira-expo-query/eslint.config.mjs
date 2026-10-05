import { pure } from '@danbro96/lupira-config-eslint';

export default pure({
  element: 'query',
  react: true,
  allowModules: [
    '@react-native-community/netinfo',
    '@tanstack/query-async-storage-persister',
    '@tanstack/react-query',
    '@tanstack/react-query-persist-client',
    'expo-sqlite',
    'react',
    'react-native',
  ],
});
