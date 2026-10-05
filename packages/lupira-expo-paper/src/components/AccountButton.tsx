import { useState } from 'react';
import { Pressable } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NavigationProp } from '@react-navigation/native';
import { Avatar } from 'react-native-paper';
import { ActionMenu } from './ActionMenu.tsx';
import { useConfirm } from './ConfirmDialog.tsx';
import { useColors } from '../theme/useColors.ts';

interface Props {
  name: string;
  sub?: string;
  onSignOut: () => void;
  signOutLabel?: string;
  signOutMessage?: string;
}

/** The one account affordance: an avatar in the navigator header that opens Settings and Sign out.
 *  `navigate` bubbles to the stack that owns the `Settings` route. */
export function AccountButton({ name, sub, onSignOut, signOutLabel = 'Sign out', signOutMessage = 'You will need to sign in again.' }: Props) {
  const navigation = useNavigation<NavigationProp<Record<string, undefined>>>();
  const confirm = useConfirm();
  const c = useColors();
  const [open, setOpen] = useState(false);

  async function signOut() {
    const ok = await confirm({ title: `${signOutLabel}?`, message: signOutMessage, confirmLabel: signOutLabel, destructive: true });
    if (ok) onSignOut();
  }

  return (
    <>
      <Pressable onPress={() => setOpen(true)} accessibilityRole="button" accessibilityLabel="Account" hitSlop={8}>
        <Avatar.Text size={32} label={name.trim().charAt(0).toUpperCase() || '?'} color={c.onPrimary} style={{ backgroundColor: c.primary }} />
      </Pressable>
      <ActionMenu
        visible={open}
        title={sub ? `${name} · ${sub}` : name}
        onClose={() => setOpen(false)}
        actions={[
          { label: 'Settings', onPress: () => navigation.navigate('Settings') },
          { label: signOutLabel, destructive: true, onPress: () => void signOut() },
        ]}
      />
    </>
  );
}
