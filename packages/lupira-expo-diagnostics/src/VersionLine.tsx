import { StyleSheet } from 'react-native';
import { Text } from 'react-native-paper';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { useColors } from '@danbro96/lupira-expo-paper/theme/useColors';
import { APP_NAME, APP_VERSION, UPDATE_LABEL } from './buildInfo.ts';

/** The About line closing every Settings screen, e.g. "Lupira Maps 1.0.0 · OTA 3fa9c1d2": the app's name and
 *  marketing version from its Expo config, then which update is running. */
export function VersionLine() {
  const c = useColors();
  return <Text style={[styles.line, { color: c.textSubtle }]}>{APP_NAME} {APP_VERSION} · {UPDATE_LABEL}</Text>;
}

const styles = StyleSheet.create({
  line: { fontSize: 12, textAlign: 'center', paddingVertical: spacing.lg },
});
