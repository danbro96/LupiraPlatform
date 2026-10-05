import { View } from 'react-native';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { Button } from './Button.tsx';
import { useConfirm } from './ConfirmDialog.tsx';

/** The sign-out row of a Settings screen's Account section: a destructive button behind a confirm dialog. */
export function SignOutButton({ onSignOut, label = 'Sign out', message = 'You will need to sign in again.' }: {
  onSignOut: () => void;
  label?: string;
  message?: string;
}) {
  const confirm = useConfirm();

  async function press() {
    const ok = await confirm({ title: `${label}?`, message, confirmLabel: label, destructive: true });
    if (ok) onSignOut();
  }

  return (
    <View style={{ paddingHorizontal: spacing.lg, paddingVertical: spacing.sm }}>
      <Button title={label} variant="destructive" onPress={() => void press()} />
    </View>
  );
}
