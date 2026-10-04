# @danbro96/lupira-expo-paper

react-native-paper (MD3) UI kit for the Lupira Expo apps.
`export const { paperLight, paperDark, navLight, navDark } = createPaperThemes(lightColors, darkColors);`
then `<PaperProvider theme={…} settings={paperSettings}>`. Read the app palette with `useColors<Palette>()`.
Mount `ConfirmDialogHost` and `ToastHost` once near the root.
