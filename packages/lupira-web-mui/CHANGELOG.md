# Changelog

## 0.3.0

- `EntryCodes`: a place's door codes over `EntryCode[]`, masked until clicked; a shown code copies and, with `editable`, opens one add/edit form (label, code, note) that also removes behind a confirm. `onSave` gets an `EntryCodeDraft` whose `id` is null for a new code.
- `useCopy`: copy to the clipboard with a snackbar confirmation; needs `SnackbarHost`.

## 0.2.1

- Accepts `@danbro96/lupira-tokens-core` ^0.3.0.

## 0.2.0

- `icons`: `AirportIcon`, `BarIcon`, `BusStopIcon`, `CafeIcon`, `ChurchIcon`, `GovernmentIcon`, `GroceryIcon`, `GymIcon`, `HospitalIcon`, `LandmarkIcon`, `ParkIcon`, `PharmacyIcon`, `SchoolIcon`, `StationIcon`, `StoreIcon`.

## 0.1.0

- `theme`: `createLupiraMuiTheme({ light, dark }, extras?)`, `cssVars`, `ThemeColors`, the `border` / `text.subtle` palette augmentation.
- `icons`: the estate's MUI icon vocabulary.
- `SnackbarHost`: `SnackbarHost`, `useSnackbar`, `SnackAction`.
- `Centered`: full-height centered message.
