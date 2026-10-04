import { createTheme, type BreakpointsOptions, type Components, type PaletteOptions, type Theme } from '@mui/material/styles';
import type { Palette as CorePalette } from '@danbro96/lupira-tokens-core/color';
import { radii, spacing } from '@danbro96/lupira-tokens-core/spacing';
import { FONT_FAMILY } from '@danbro96/lupira-tokens-core/typography';

declare module '@mui/material/styles' {
  interface Palette {
    border: string;
  }
  interface PaletteOptions {
    border?: string;
  }
  interface TypeText {
    subtle: string;
  }
}

export type ThemeColors = CorePalette & { warning?: string; success?: string };

export interface LupiraMuiThemeExtras<P extends ThemeColors> {
  palette?: (c: P) => PaletteOptions;
  rootVars?: { light: Record<string, string>; dark?: Record<string, string> };
  baseline?: Record<string, unknown>;
  breakpoints?: BreakpointsOptions;
  components?: Components<Omit<Theme, 'components'>>;
}

// Custom props must carry units — Emotion serializes them verbatim.
export function cssVars(values: Record<string, string | number>, prefix: string, unit = ''): Record<string, string> {
  return Object.fromEntries(Object.entries(values).map(([k, v]) => [`--${prefix}-${k.toLowerCase()}`, `${v}${unit}`]));
}

function palette<P extends ThemeColors>(c: P, extra?: (c: P) => PaletteOptions): PaletteOptions {
  return {
    background: { default: c.bg, paper: c.surface },
    primary: { main: c.primary, contrastText: c.onPrimary },
    divider: c.divider,
    border: c.border,
    text: { primary: c.text, secondary: c.textMuted, disabled: c.textDisabled, subtle: c.textSubtle },
    error: { main: c.danger },
    ...(c.warning ? { warning: { main: c.warning } } : {}),
    ...(c.success ? { success: { main: c.success } } : {}),
    ...extra?.(c),
  };
}

export function createLupiraMuiTheme<P extends ThemeColors>(
  tokens: { light: P; dark: P },
  extras: LupiraMuiThemeExtras<P> = {},
): Theme {
  const { rootVars } = extras;
  return createTheme({
    // 'media' = system-driven scheme; MUI emits the dark var overrides in a prefers-color-scheme block.
    cssVariables: { colorSchemeSelector: 'media' },
    colorSchemes: {
      light: { palette: palette(tokens.light, extras.palette) },
      dark: { palette: palette(tokens.dark, extras.palette) },
    },
    // No webfont is loaded; without this MUI assumes Roboto and changes every font.
    typography: { fontFamily: FONT_FAMILY },
    shape: { borderRadius: radii.md },
    spacing: spacing.sm,
    ...(extras.breakpoints ? { breakpoints: extras.breakpoints } : {}),
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          ...(rootVars
            ? { ':root': { ...rootVars.light, ...(rootVars.dark ? { '@media (prefers-color-scheme: dark)': rootVars.dark } : {}) } }
            : {}),
          ...extras.baseline,
        },
      },
      // The apps are uniformly compact; opt out per-instance rather than repeating size="small".
      MuiButton: { defaultProps: { size: 'small' } },
      MuiIconButton: { defaultProps: { size: 'small' } },
      MuiTextField: { defaultProps: { size: 'small' } },
      MuiChip: { defaultProps: { size: 'small' } },
      MuiToggleButtonGroup: { defaultProps: { size: 'small' } },
      MuiLink: { defaultProps: { underline: 'hover' } },
      ...extras.components,
    },
  });
}
