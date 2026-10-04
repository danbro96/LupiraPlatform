import { useTheme, type MD3Theme } from 'react-native-paper';
import type { Palette } from '@danbro96/lupira-tokens-core/color';

/** The active palette, read off the Paper theme PaperProvider is actually holding.
 *  Deriving it from useColorScheme() again would be a second source that can disagree. */
export function useColors<P extends Palette = Palette>(): P {
  return useTheme<MD3Theme & { colors: P }>().colors;
}
