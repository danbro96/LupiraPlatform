# Changelog

## 0.5.0
- `IconButton` forwards Paper's own props (`style`, `disabled`, `selected`, `size`) and no longer forces `margin: 0`.
- `AccountButton` sets no margin of its own; its touch target comes from `hitSlop`.
- `ScreenToolbar` and `Sheet` take their spacing and corner radius from the tokens; `Sheet` is one `maxHeight` (80%) for both anchors and its title is Paper's `titleMedium`.

## 0.4.0
- Removed `VersionLine`; it lives in `lupira-expo-diagnostics`, which owns the build info it shows.

## 0.3.0
- `TextField` no longer sets `flex: 1`; it sizes itself. Put `style={{ flex: 1 }}` on a field that shares a row. `fieldGap` (theme/styles) spaces stacked fields.

## 0.2.0
- `useStackScreenOptions`: native-stack `screenOptions` that pad each screen above the system navigation bar (edge-to-edge).
- `Screen` (app background + optional status strip), `HeaderActions` (≤2 icons + overflow menu), `AccountButton` (avatar menu: Settings, Sign out), `IdentityHeader`, `VersionLine`, `SettingsNote`, `SettingsAction`, `Sheet`.
- Removed `SettingsButton`; use `AccountButton`. `ICONS`/`configureIcons`: `settings` dropped, `more` added.

## 0.1.2
- `navLight`/`navDark` are typed as `@react-navigation/native`'s `Theme`, so apps on React Navigation 7.3.15+ need no cast.

## 0.1.1
- Depends on `@danbro96/lupira-tokens-core` ^0.2.0.

## 0.1.0

- Components: `Button`, `ConfirmDialogHost`/`useConfirm`, `IconButton`, `ToastHost`/`useToastClearance`, `SettingsButton`, `Glyph`, `ScreenToolbar`, `TextField`, `SegmentedPicker`, `ActionMenu`, `ChoiceChips`.
- Theme: `useColors<P>()`, `createPaperThemes(light, dark)`, `paperSettings`, `cardSurface`.
- Hooks: `useLatestCallback`, `useBackDismiss`.
- `configureIcons()` for the glyphs the kit draws itself (`check`, `settings`).
