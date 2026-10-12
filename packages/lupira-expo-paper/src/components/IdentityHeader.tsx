import { Avatar, List } from 'react-native-paper';
import { useColors } from '../theme/useColors.ts';

/** The top of a Settings screen: who this device or account is. */
export function IdentityHeader({ name, sub }: { name: string; sub?: string }) {
  const c = useColors();
  return (
    <List.Item
      title={name}
      description={sub}
      descriptionStyle={{ color: c.textMuted }}
      left={({ style }) => (
        <Avatar.Text size={40} label={name.trim().charAt(0).toUpperCase() || '?'} color={c.onPrimary} style={[style, { backgroundColor: c.primary }]} />
      )}
    />
  );
}
