import { StyleSheet } from 'react-native';
import { Text } from 'react-native-paper';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { useColors } from '../theme/useColors.ts';

/** The About line closing every Settings screen, e.g. "Lupira Maps 1.2.0 · ota-3". */
export function VersionLine({ app, version, updateLabel }: { app: string; version: string; updateLabel: string }) {
  const c = useColors();
  return <Text style={[styles.line, { color: c.textSubtle }]}>{app} {version} · {updateLabel}</Text>;
}

const styles = StyleSheet.create({
  line: { fontSize: 12, textAlign: 'center', paddingVertical: spacing.lg },
});
