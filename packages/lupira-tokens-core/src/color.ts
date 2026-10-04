// The estate's shared core, identical across the Lupira frontends — see DevOps
// Guides/design-tokens.md. Product-specific semantics extend it in the app's own Palette.
export interface Palette {
  bg: string;
  surface: string;
  primary: string;
  onPrimary: string;
  border: string;
  divider: string;
  text: string;
  textMuted: string;
  textSubtle: string;
  textDisabled: string;
  danger: string;
  brand: string;
}

export const lightColors: Palette = {
  bg: '#ffffff',
  surface: '#f5f6f8',
  primary: '#0d9488',
  onPrimary: '#ffffff',
  border: '#d4d8e0',
  divider: '#e3e6ec',
  text: '#1c2230',
  textMuted: '#6e7686',
  textSubtle: '#8a909c',
  textDisabled: '#9aa0ac',
  danger: '#b3261e',
  brand: '#E76F51',
};

export const darkColors: Palette = {
  bg: '#14171c',
  surface: '#1e232b',
  primary: '#2dd4bf',
  onPrimary: '#042f2e',
  border: '#2c333d',
  divider: '#252b33',
  text: '#e6e9ee',
  textMuted: '#9aa3b2',
  textSubtle: '#7c8492',
  textDisabled: '#5b626e',
  danger: '#f2675e',
  brand: '#E76F51',
};

/** Over photos and behind sheets. The same in both schemes: what lies underneath sets the contrast. */
export const SCRIM = {
  backdrop: 'rgba(0, 0, 0, 0.4)',
  onImage: 'rgba(0, 0, 0, 0.6)',
  onImageHover: 'rgba(0, 0, 0, 0.8)',
  textOnImage: '#ffffff',
} as const;

/** A '#rrggbb' colour with an alpha, as '#rrggbbaa'. */
export function withAlpha(hex: string, alpha: number): string {
  return `${hex.slice(0, 7)}${Math.round(Math.min(1, Math.max(0, alpha)) * 255).toString(16).padStart(2, '0')}`;
}
