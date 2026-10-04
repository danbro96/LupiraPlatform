import { zoomForSpan } from '@danbro96/lupira-domain-maps/mapZoom';
import type { MapTheme } from '@danbro96/lupira-tokens-map/map';
import Box from '@mui/material/Box';
import { useTheme } from '@mui/material/styles';
import { Map as MapLibreMap, Marker, type StyleSpecification } from 'maplibre-gl';
import { useEffect, useRef } from 'react';
import { fallbackStyle, loadMapStyle } from './mapStyle.ts';
import { useMapTheme } from './useMapTheme.ts';
import './maplibreSetup.ts';

// Loading a style probes the basemap's tiles; one probe per theme serves every thumbnail on the page.
const styles = new Map<string, Promise<StyleSpecification>>();
const styleFor = (theme: MapTheme, geoBase: string) => {
  const key = `${theme} ${geoBase}`;
  let style = styles.get(key);
  if (!style) {
    style = loadMapStyle(theme, geoBase).catch(() => fallbackStyle(theme, geoBase));
    styles.set(key, style);
  }
  return style;
};

/** A still basemap thumbnail around one point, zoomed so `spanM` metres fit across it. Lazy-load it so the
 *  maplibre chunk only arrives with the first tile; each one holds a WebGL context, so detail views only. */
export default function MiniMap({ point, spanM, width, height, geoBase }: {
  point: { lat: number; lon: number };
  spanM: number;
  width: number;
  height: number;
  geoBase: string;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const theme = useMapTheme();
  const pin = useTheme().palette.primary.main;

  useEffect(() => {
    let disposed = false;
    let map: MapLibreMap | undefined;
    void styleFor(theme, geoBase).then((style) => {
      if (disposed || !ref.current) return;
      map = new MapLibreMap({
        container: ref.current,
        style,
        center: [point.lon, point.lat],
        zoom: zoomForSpan(spanM, width, point.lat),
        interactive: false,
        attributionControl: false,
      });
      new Marker({ color: pin, scale: 0.6 }).setLngLat([point.lon, point.lat]).addTo(map);
    });
    return () => {
      disposed = true;
      map?.remove();
    };
  }, [point.lat, point.lon, spanM, width, theme, pin, geoBase]);

  return <Box ref={ref} sx={{ width, height, flex: 'none', borderRadius: 1, overflow: 'hidden', bgcolor: 'action.hover' }} />;
}
