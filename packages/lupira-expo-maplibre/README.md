# @danbro96/lupira-expo-maplibre

MapLibre React Native wiring for the Lupira Expo apps: `/mapAuthHeader`. JS only, so it leaves the app's native fingerprint alone.

- `installMapAuthHeader({ subscribeToken, getToken, apiUrl })`: sends `Authorization: Bearer <token>` on native map requests (tiles, glyphs, sprites) to the API origin only, follows token changes, and returns an uninstall that removes the header.

```ts
const uninstall = installMapAuthHeader({
  subscribeToken: (listener) => useAuth.subscribe(listener),
  getToken: () => useAuth.getState().token,
  apiUrl: () => useAuth.getState().apiUrl,
});
```
