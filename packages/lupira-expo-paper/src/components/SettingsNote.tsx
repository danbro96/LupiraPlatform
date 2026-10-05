import type { ReactNode } from 'react';
import { StyleSheet } from 'react-native';
import { Text } from 'react-native-paper';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { useColors } from '../theme/useColors.ts';

/** Explanatory text under a settings row, lined up with the row's title. */
export function SettingsNote({ children }: { children: ReactNode }) {
  const c = useColors();
  return <Text style={[styles.note, { color: c.textMuted }]}>{children}</Text>;
}

const styles = StyleSheet.create({
  note: { fontSize: 13, paddingHorizontal: spacing.lg, paddingBottom: spacing.sm },
});
