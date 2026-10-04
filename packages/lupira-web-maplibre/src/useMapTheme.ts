import type { MapTheme } from '@danbro96/lupira-tokens-map/map';
import { useEffect, useState } from 'react';

export function useMapTheme(): MapTheme {
  const [theme, setTheme] = useState<MapTheme>(() =>
    window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const onChange = (e: MediaQueryListEvent) => setTheme(e.matches ? 'dark' : 'light');
    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);
  return theme;
}
