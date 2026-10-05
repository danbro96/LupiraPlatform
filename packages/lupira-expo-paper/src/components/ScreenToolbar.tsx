import type { ReactNode } from 'react';
import { StyleSheet, View } from 'react-native';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';

/** A screen's own controls, in a row under the navigator's header — never instead of it. Search
 *  fields, period navigators and filters live here; navigation chrome stays with the header. */
export function ScreenToolbar({ children }: { children: ReactNode }) {
  return <View style={styles.toolbar}>{children}</View>;
}

const styles = StyleSheet.create({
  toolbar: { flexDirection: 'row', alignItems: 'center', paddingHorizontal: spacing.md, paddingVertical: spacing.sm, gap: spacing.sm },
});
