import { Pressable, StyleSheet, View } from 'react-native';
import { ProgressBar, Text } from 'react-native-paper';
import type { BannerPalette, Palette } from '@danbro96/lupira-tokens-core/color';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { ICONS } from '../icons.ts';
import type { StatusBannerKind, StatusBannerState } from '../status.ts';
import { useColors } from '../theme/useColors.ts';
import { Glyph } from './Glyph.tsx';

/** Connection/sync state for `Screen`'s `banner` slot, and nothing when `state` is null. A quiet state is only an
 *  activity line over the top edge, so it never shifts the screen; `progress={false}` drops that line. */
export function StatusBanner({ state, onPress, progress = true }: {
  state: StatusBannerState | null;
  onPress?: () => void;
  progress?: boolean;
}) {
  const c = useColors<Palette & BannerPalette>();
  if (!state) return null;

  if (state.quiet) {
    if (!progress) return null;
    return (
      <View style={styles.overlay} accessibilityLabel={state.text}>
        <ProgressBar indeterminate color={c.bannerSyncing} style={styles.bar} />
      </View>
    );
  }

  return (
    <Pressable
      style={[styles.strip, { backgroundColor: backgroundOf(c, state.kind) }]}
      onPress={onPress}
      disabled={!onPress}
      accessibilityRole={onPress ? 'button' : undefined}
      accessibilityLiveRegion="polite"
    >
      <Text variant="bodySmall" style={[styles.text, { color: c.onStatus }]} numberOfLines={1}>
        <Glyph name={iconOf(state.kind)} size={14} />  {state.text}
      </Text>
      {onPress ? <Text style={{ color: c.onStatus }}><Glyph name={ICONS.chevronRight} size={16} /></Text> : null}
      {progress && state.kind === 'syncing' ? <ProgressBar indeterminate color={c.onStatus} style={[styles.bar, styles.stripBar]} /> : null}
    </Pressable>
  );
}

function backgroundOf(c: BannerPalette, kind: StatusBannerKind): string {
  if (kind === 'syncing') return c.bannerSyncing;
  return kind === 'offline' ? c.bannerOffline : c.bannerUnreachable;
}

function iconOf(kind: StatusBannerKind) {
  if (kind === 'syncing') return ICONS.sync;
  return kind === 'offline' || kind === 'unreachable' ? ICONS.cloudOff : ICONS.alert;
}

const styles = StyleSheet.create({
  // Above the screen's later siblings, which would otherwise paint over it.
  overlay: { position: 'absolute', top: 0, left: 0, right: 0, zIndex: 1, pointerEvents: 'none' },
  bar: { height: 2 },
  strip: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, minHeight: 28, paddingHorizontal: spacing.md, paddingVertical: spacing.xs },
  text: { flex: 1 },
  stripBar: { position: 'absolute', left: 0, right: 0, bottom: 0 },
});
