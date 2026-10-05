import { useState } from 'react';
import type { ComponentProps } from 'react';
import { StyleSheet, View } from 'react-native';
import type MaterialIcons from '@expo/vector-icons/MaterialIcons';
import { ActionMenu, type ActionItem } from './ActionMenu.tsx';
import { IconButton } from './IconButton.tsx';
import { ICONS } from '../icons.ts';

export interface HeaderAction {
  icon: ComponentProps<typeof MaterialIcons>['name'];
  label: string;
  onPress: () => void;
}

/** A header's own actions: at most two icons, anything further goes in the overflow menu. */
export function HeaderActions({ actions, overflow }: { actions: HeaderAction[]; overflow?: ActionItem[] }) {
  const [open, setOpen] = useState(false);
  if (__DEV__ && actions.length > 2) throw new Error('HeaderActions shows at most two actions; move the rest to `overflow`.');
  return (
    <View style={styles.row}>
      {actions.map(a => (
        <IconButton key={a.label} name={a.icon} accessibilityLabel={a.label} onPress={a.onPress} />
      ))}
      {overflow?.length ? (
        <>
          <IconButton name={ICONS.more} accessibilityLabel="More" onPress={() => setOpen(true)} />
          <ActionMenu visible={open} actions={overflow} onClose={() => setOpen(false)} />
        </>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center' },
});
