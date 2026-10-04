import { MD3DarkTheme, MD3LightTheme, adaptNavigationTheme } from 'react-native-paper';
import { DarkTheme as NavDarkBase, DefaultTheme as NavLightBase } from '@react-navigation/native';
import type { Palette } from '@danbro96/lupira-tokens-core/color';
import { radii } from '@danbro96/lupira-tokens-core/spacing';

// The whole palette rides on the theme: MD3 gets its own slot names, and the app's vocabulary
// (bg/text plus any product keys) comes along so useColors() can read it back off the theme
// rather than deriving the scheme a second time.
function md3Colors<P extends Palette>(p: P) {
  return {
    ...p,
    primary: p.primary,
    onPrimary: p.onPrimary,
    background: p.bg,
    onBackground: p.text,
    surface: p.surface,
    onSurface: p.text,
    onSurfaceVariant: p.textMuted,
    outline: p.border,
    outlineVariant: p.divider,
    error: p.danger,
    // SegmentedButtons paints its selected segment from these.
    secondaryContainer: p.primary,
    onSecondaryContainer: p.onPrimary,
  };
}

export function createPaperThemes<P extends Palette>(lightColors: P, darkColors: P) {
  const paperLight = { ...MD3LightTheme, roundness: radii.sm, colors: { ...MD3LightTheme.colors, ...md3Colors(lightColors) } };
  const paperDark = { ...MD3DarkTheme, roundness: radii.sm, colors: { ...MD3DarkTheme.colors, ...md3Colors(darkColors) } };
  const adapted = adaptNavigationTheme({
    reactNavigationLight: NavLightBase,
    reactNavigationDark: NavDarkBase,
    materialLight: paperLight,
    materialDark: paperDark,
  });
  return { paperLight, paperDark, navLight: adapted.LightTheme, navDark: adapted.DarkTheme };
}

export type AppTheme<P extends Palette = Palette> = ReturnType<typeof createPaperThemes<P>>['paperLight'];
