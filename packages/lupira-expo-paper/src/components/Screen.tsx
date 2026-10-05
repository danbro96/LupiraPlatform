import type { ReactNode } from 'react';
import { View } from 'react-native';
import { useColors } from '../theme/useColors.ts';

/** A screen's root: the app background, with an optional status strip (sync banner) above the content. */
export function Screen({ banner, children }: { banner?: ReactNode; children: ReactNode }) {
  const c = useColors();
  return (
    <View style={{ flex: 1, backgroundColor: c.bg }}>
      {banner}
      {children}
    </View>
  );
}
