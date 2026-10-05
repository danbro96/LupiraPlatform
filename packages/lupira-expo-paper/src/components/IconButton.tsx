import type { ComponentProps } from 'react';
import MaterialIcons from '@expo/vector-icons/MaterialIcons';
import { IconButton as PaperIconButton } from 'react-native-paper';
import { useColors } from '../theme/useColors.ts';

type Props = Omit<ComponentProps<typeof PaperIconButton>, 'icon' | 'iconColor' | 'theme' | 'onPress' | 'accessibilityLabel'> & {
  name: ComponentProps<typeof MaterialIcons>['name'];
  onPress: () => void;
  accessibilityLabel: string;
  color?: string;
};

/** A tappable icon, primarily for navigation headers. Resolves through PaperProvider's
 *  `settings.icon` override, so `name` is a MaterialIcons glyph — pass one from `ui/icons.ts`.
 *  Everything else (`style`, `disabled`, `selected`, `size`) is Paper's own. */
export function IconButton({ name, color, size = 24, ...props }: Props) {
  const c = useColors();
  return <PaperIconButton {...props} icon={name} size={size} iconColor={color ?? c.primary} />;
}
