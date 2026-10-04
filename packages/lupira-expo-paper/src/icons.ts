import type MaterialIcons from '@expo/vector-icons/MaterialIcons';
import type { ComponentProps } from 'react';

type MaterialGlyph = ComponentProps<typeof MaterialIcons>['name'];

export interface KitIcons {
  check: MaterialGlyph;
  settings: MaterialGlyph;
}

/** The glyphs the kit draws on its own; an app whose registry maps these concepts elsewhere calls
 *  `configureIcons` once at startup, before the first render. */
export const ICONS: KitIcons = { check: 'check', settings: 'settings' };

export function configureIcons(overrides: Partial<KitIcons>): void {
  Object.assign(ICONS, overrides);
}
