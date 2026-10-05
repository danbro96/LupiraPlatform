import type { ReactNode } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import { Text } from 'react-native-paper';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import type { Palette } from '@danbro96/lupira-tokens-core/color';
import { useColors } from '../theme/useColors.ts';

/** A line that needs attention and fixes itself on tap — a missing permission, a failed upload, an erase.
 *  Needs an app palette that carries `warning`. */
export function SettingsAction({ tone = 'warning', onPress, children }: {
  tone?: 'warning' | 'danger';
  onPress: () => void;
  children: ReactNode;
}) {
  const c = useColors<Palette & { warning: string }>();
  return (
    <Pressable onPress={onPress} accessibilityRole="button" style={styles.action}>
      <Text style={[styles.actionText, { color: tone === 'danger' ? c.danger : c.warning }]}>{children}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  action: { paddingHorizontal: spacing.lg, paddingVertical: spacing.sm },
  actionText: { fontSize: 14 },
});
