import { useEffect, type ReactNode } from 'react';
import { BackHandler, Pressable, StyleSheet } from 'react-native';
import { Portal, Text } from 'react-native-paper';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { SCRIM } from '@danbro96/lupira-tokens-core/color';
import { radii, spacing } from '@danbro96/lupira-tokens-core/spacing';
import { useColors } from '../theme/useColors.ts';

/** A modal picker sheet: bottom-anchored, or top-anchored when it holds a search box so the keyboard never
 *  covers the results. Hardware back closes the sheet rather than the screen under it. */
export function Sheet({ title, anchor = 'bottom', onDismiss, children }: {
  title?: string;
  anchor?: 'top' | 'bottom';
  onDismiss: () => void;
  children: ReactNode;
}) {
  const c = useColors();
  const insets = useSafeAreaInsets();

  useEffect(() => {
    const sub = BackHandler.addEventListener('hardwareBackPress', () => {
      onDismiss();
      return true;
    });
    return () => sub.remove();
  }, [onDismiss]);

  const top = anchor === 'top';
  return (
    <Portal>
      <Pressable style={[styles.backdrop, { justifyContent: top ? 'flex-start' : 'flex-end' }]} onPress={onDismiss}>
        <Pressable
          style={[
            styles.sheet,
            top ? styles.topCorners : styles.bottomCorners,
            { backgroundColor: c.surface, paddingTop: top ? insets.top + spacing.sm : spacing.lg, paddingBottom: top ? spacing.md : insets.bottom + spacing.md },
          ]}
        >
          {title && <Text variant="titleMedium" style={styles.title}>{title}</Text>}
          {children}
        </Pressable>
      </Pressable>
    </Portal>
  );
}

const styles = StyleSheet.create({
  backdrop: { flex: 1, backgroundColor: SCRIM.backdrop },
  sheet: { paddingHorizontal: spacing.lg, maxHeight: '80%' },
  bottomCorners: { borderTopLeftRadius: radii.lg, borderTopRightRadius: radii.lg },
  topCorners: { borderBottomLeftRadius: radii.lg, borderBottomRightRadius: radii.lg },
  title: { marginBottom: spacing.xs },
});
