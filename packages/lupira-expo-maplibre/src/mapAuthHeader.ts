import { TransformRequestManager } from '@maplibre/maplibre-react-native';

const AUTH_HEADER_ID = 'lupira-auth';

const escapeRegex = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

export interface MapAuthHeaderSource {
  subscribeToken: (listener: () => void) => () => void;
  getToken: () => string | null;
  apiUrl: () => string;
}

/** Scoped to the API origin so presigned or third-party map URLs never receive the bearer. */
export function installMapAuthHeader({ subscribeToken, getToken, apiUrl }: MapAuthHeaderSource): () => void {
  let applied: { token: string | null; origin: string } | undefined;

  const apply = () => {
    const token = getToken() || null;
    const origin = apiUrl().replace(/\/$/, '');
    if (applied && applied.token === token && (token === null || applied.origin === origin)) return;
    applied = { token, origin };
    if (token === null) {
      TransformRequestManager.removeHeader(AUTH_HEADER_ID);
      return;
    }
    TransformRequestManager.addHeader({
      id: AUTH_HEADER_ID,
      name: 'Authorization',
      value: `Bearer ${token}`,
      match: `^${escapeRegex(origin)}/`,
    });
  };

  apply();
  const unsubscribe = subscribeToken(apply);
  return () => {
    unsubscribe();
    applied = undefined;
    TransformRequestManager.removeHeader(AUTH_HEADER_ID);
  };
}
