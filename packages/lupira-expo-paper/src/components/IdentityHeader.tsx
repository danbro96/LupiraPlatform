import { StyleSheet, View } from 'react-native';
import { Avatar, Text } from 'react-native-paper';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { useColors } from '../theme/useColors.ts';

/** The top of a Settings screen: who this device or account is. */
export function IdentityHeader({ name, sub }: { name: string; sub?: string }) {
  const c = useColors();
  return (
    <View style={styles.identity}>
      <Avatar.Text size={72} label={name.trim().charAt(0).toUpperCase() || '?'} color={c.onPrimary} style={{ backgroundColor: c.primary }} />
      <Text variant="titleLarge" style={styles.name}>{name}</Text>
      {sub ? <Text variant="bodySmall" style={{ color: c.textMuted }}>{sub}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  identity: { alignItems: 'center', paddingTop: spacing.xl, paddingBottom: spacing.lg, gap: spacing.xs },
  name: { marginTop: spacing.md },
});
