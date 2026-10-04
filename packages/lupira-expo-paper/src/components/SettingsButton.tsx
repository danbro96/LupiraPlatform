import { useNavigation } from '@react-navigation/native';
import type { NavigationProp } from '@react-navigation/native';
import { IconButton } from './IconButton.tsx';
import { ICONS } from '../icons.ts';

/** The one settings affordance. Settings lives off the tab bar on a parent stack; `navigate` bubbles
 *  to it, so this works identically from a tab screen and from the root stack. */
export function SettingsButton() {
  const navigation = useNavigation<NavigationProp<Record<string, undefined>>>();
  return (
    <IconButton
      name={ICONS.settings}
      accessibilityLabel="Settings"
      onPress={() => navigation.navigate('Settings')}
    />
  );
}
