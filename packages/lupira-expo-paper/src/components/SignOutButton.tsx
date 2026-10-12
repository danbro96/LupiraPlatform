import { List } from 'react-native-paper';
import { ICONS } from '../icons.ts';
import { useColors } from '../theme/useColors.ts';
import { useConfirm } from './ConfirmDialog.tsx';

/** The sign-out row of a Settings screen's Account section: a danger-tinted row behind a confirm dialog. */
export function SignOutButton({ onSignOut, label = 'Sign out', message = 'You will need to sign in again.' }: {
  onSignOut: () => void;
  label?: string;
  message?: string;
}) {
  const c = useColors();
  const confirm = useConfirm();

  async function press() {
    const ok = await confirm({ title: `${label}?`, message, confirmLabel: label, destructive: true });
    if (ok) onSignOut();
  }

  return (
    <List.Item
      title={label}
      titleStyle={{ color: c.danger }}
      left={props => <List.Icon {...props} icon={ICONS.signOut} color={c.danger} />}
      onPress={() => void press()}
      accessibilityRole="button"
    />
  );
}
