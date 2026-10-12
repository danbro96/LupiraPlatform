import type MaterialIcons from '@expo/vector-icons/MaterialIcons';
import type { ComponentProps } from 'react';

type MaterialGlyph = ComponentProps<typeof MaterialIcons>['name'];

export interface KitIcons {
  alert: MaterialGlyph;
  check: MaterialGlyph;
  chevronRight: MaterialGlyph;
  cloudOff: MaterialGlyph;
  more: MaterialGlyph;
  signOut: MaterialGlyph;
  sync: MaterialGlyph;
}

/** The glyphs the kit draws on its own; an app whose registry maps these concepts elsewhere calls
 *  `configureIcons` once at startup, before the first render. */
export const ICONS: KitIcons = {
  alert: 'error-outline',
  check: 'check',
  chevronRight: 'chevron-right',
  cloudOff: 'cloud-off',
  more: 'more-vert',
  signOut: 'logout',
  sync: 'sync',
};

export function configureIcons(overrides: Partial<KitIcons>): void {
  Object.assign(ICONS, overrides);
}
