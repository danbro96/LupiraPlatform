import type { GeoPoint } from '@danbro96/lupira-domain-places/geo';
import { formatCoords } from '@danbro96/lupira-domain-places/places';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Typography from '@mui/material/Typography';
import { useTheme } from '@mui/material/styles';
import { Map as MapLibreMap, Marker, NavigationControl } from 'maplibre-gl';
import 'maplibre-gl/dist/maplibre-gl.css';
import { useEffect, useEffectEvent, useRef, useState, type ReactNode } from 'react';
import { fallbackStyle, loadMapStyle } from './mapStyle.ts';
import { pinView } from './pinView.ts';
import { useMapTheme } from './useMapTheme.ts';
import './maplibreSetup.ts';

export interface MapPinDialogProps {
  open: boolean;
  /** The initial view; the map owns the camera after that. */
  start: { lat: number; lon: number; zoom?: number };
  geoBase: string;
  onCancel: () => void;
  onConfirm: (point: GeoPoint) => void;
  /** Fired on open (with `initialPin` or null), on placing and after each drag. */
  onPinChange?: (point: GeoPoint | null) => void;
  /** Read on open only; without it Confirm waits for a pin, so the fallback view can't be confirmed. */
  initialPin?: GeoPoint | null;
  title?: string;
  /** Shown above the map, e.g. a name field. */
  children?: ReactNode;
  confirmDisabled?: boolean;
}

/** Pick coordinates by clicking the map, then dragging the marker. Lazy-load it so the maplibre chunk only arrives
 *  with the first pin. */
export default function MapPinDialog({ open, title, onCancel, ...picker }: MapPinDialogProps) {
  return (
    <Dialog open={open} onClose={onCancel} maxWidth="md" fullWidth>
      <DialogTitle>{title ?? 'Drop a pin'}</DialogTitle>
      <PinPicker onCancel={onCancel} {...picker} />
    </Dialog>
  );
}

function PinPicker({ start, geoBase, onCancel, onConfirm, onPinChange, initialPin, children, confirmDisabled }: Omit<MapPinDialogProps, 'open' | 'title'>) {
  const ref = useRef<HTMLDivElement>(null);
  const [pin, setPin] = useState<GeoPoint | null>(initialPin ?? null);
  const theme = useMapTheme();
  const color = useTheme().palette.primary.main;
  const setup = useRef({ start, initialPin, geoBase, theme, color });
  const changePin = useEffectEvent((point: GeoPoint | null) => {
    setPin(point);
    onPinChange?.(point);
  });

  useEffect(() => {
    const container = ref.current;
    if (!container) return;
    const { start: view, initialPin: initial, geoBase: base, theme: mapTheme, color: pinColor } = setup.current;
    changePin(initial ?? null);
    let disposed = false;
    let map: MapLibreMap | undefined;
    loadMapStyle(mapTheme, base).catch(() => fallbackStyle(mapTheme, base)).then((style) => {
      if (disposed) return;
      const created = new MapLibreMap({
        container,
        style,
        ...pinView(view, initial),
        attributionControl: { compact: true },
      });
      map = created;
      created.addControl(new NavigationControl({ showCompass: false }), 'top-right');
      // The dialog can still be laying out at construction; a resize after load picks up the final size.
      created.once('load', () => created.resize());
      const marker = new Marker({ color: pinColor, draggable: true });
      if (initial) marker.setLngLat([initial.lon, initial.lat]).addTo(created);
      marker.on('dragend', () => {
        const p = marker.getLngLat();
        changePin({ lat: p.lat, lon: p.lng });
      });
      created.on('click', (e) => {
        marker.setLngLat(e.lngLat).addTo(created);
        changePin({ lat: e.lngLat.lat, lon: e.lngLat.lng });
      });
    }).catch(reportError);
    return () => {
      disposed = true;
      map?.remove();
    };
  }, []);

  return (
    <>
      <DialogContent>
        {children}
        <Box ref={ref} sx={{ height: 420, borderRadius: 1, overflow: 'hidden', bgcolor: 'action.hover' }} />
        <Typography variant="caption" sx={{ color: 'text.secondary' }} component="p">
          {pin ? formatCoords(pin.lat, pin.lon) : 'Click the map to place the pin, then drag to fine-tune.'}
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel}>Cancel</Button>
        <Button variant="contained" disabled={!pin || confirmDisabled} onClick={() => pin && onConfirm(pin)}>
          Confirm
        </Button>
      </DialogActions>
    </>
  );
}
