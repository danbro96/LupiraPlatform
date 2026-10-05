import { Pressable } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NavigationProp } from '@react-navigation/native';
import { Avatar } from 'react-native-paper';
import { useColors } from '../theme/useColors.ts';

/** The account affordance in a navigator header: an avatar that opens Settings. `navigate` bubbles to the
 *  stack that owns the `Settings` route. */
export function AccountButton({ name }: { name: string }) {
  const navigation = useNavigation<NavigationProp<Record<string, undefined>>>();
  const c = useColors();
  return (
    <Pressable onPress={() => navigation.navigate('Settings')} accessibilityRole="button" accessibilityLabel="Settings" hitSlop={8}>
      <Avatar.Text size={32} label={name.trim().charAt(0).toUpperCase() || '?'} color={c.onPrimary} style={{ backgroundColor: c.primary }} />
    </Pressable>
  );
}
