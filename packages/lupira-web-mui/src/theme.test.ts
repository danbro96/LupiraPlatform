import { darkColors, lightColors } from '@danbro96/lupira-tokens-core/color';
import { describe, expect, it } from 'vitest';
import type { Palette, Theme } from '@mui/material/styles';
import { createLupiraMuiTheme, cssVars } from './theme.ts';

const scheme = (theme: Theme, mode: 'light' | 'dark') =>
  (theme as Theme & { colorSchemes: Record<'light' | 'dark', { palette: Palette }> }).colorSchemes[mode].palette;

describe('cssVars', () => {
  it('names custom props by prefix and key, with units', () => {
    expect(cssVars({ md: 12, Family: 3 }, 'sp', 'px')).toEqual({ '--sp-md': '12px', '--sp-family': '3px' });
  });
});

describe('createLupiraMuiTheme', () => {
  it('maps the core palette into both schemes', () => {
    const theme = createLupiraMuiTheme({ light: lightColors, dark: darkColors });
    expect(scheme(theme, 'light').primary.main).toBe(lightColors.primary);
    expect(scheme(theme, 'dark').border).toBe(darkColors.border);
    expect(scheme(theme, 'light').text.subtle).toBe(lightColors.textSubtle);
  });

  it('takes the optional semantic slots and app extras', () => {
    const theme = createLupiraMuiTheme(
      { light: { ...lightColors, warning: '#f59e0b' }, dark: { ...darkColors, warning: '#fbbf24' } },
      {
        palette: (c) => ({ info: { main: c.brand } }),
        rootVars: { light: { '--cat-family': '#2e7d32' }, dark: { '--cat-family': '#6bbf6e' } },
        components: { MuiChip: { defaultProps: { size: 'medium' } } },
      },
    );
    expect(scheme(theme, 'dark').warning.main).toBe('#fbbf24');
    expect(scheme(theme, 'light').info.main).toBe(lightColors.brand);
    expect(theme.components?.MuiCssBaseline?.styleOverrides).toEqual({
      ':root': { '--cat-family': '#2e7d32', '@media (prefers-color-scheme: dark)': { '--cat-family': '#6bbf6e' } },
    });
    expect(theme.components?.MuiChip?.defaultProps?.size).toBe('medium');
    expect(theme.components?.MuiButton?.defaultProps?.size).toBe('small');
  });
});
