import { useSafeAreaInsets } from 'react-native-safe-area-context';

/** Pass as a native-stack `screenOptions`: pads every screen above the system navigation bar. The screen
 *  that hosts a tab navigator opts out (`contentStyle: { paddingBottom: 0 }`); the tab bar insets itself. */
export function useStackScreenOptions() {
  return { contentStyle: { paddingBottom: useSafeAreaInsets().bottom } };
}
