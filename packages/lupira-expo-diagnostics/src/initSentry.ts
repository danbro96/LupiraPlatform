import * as Sentry from '@sentry/react-native';
import { UPDATE_CHANNEL, UPDATE_ID } from './buildInfo.ts';

/** Sentry for every Lupira app: off without a DSN, no default PII, dev events kept apart from production, and
 *  every event tagged with the running OTA update. `options` override or extend the defaults. */
export function initSentry(dsn: string | undefined, options: Omit<Parameters<typeof Sentry.init>[0], 'dsn'> = {}): void {
  Sentry.init({
    dsn,
    enabled: !!dsn,
    tracesSampleRate: 0.2,
    sendDefaultPii: false,
    environment: __DEV__ ? 'development' : 'production',
    ...options,
  });
  Sentry.setTag('update_id', UPDATE_ID ?? 'none');
  Sentry.setTag('update_channel', UPDATE_CHANNEL ?? 'none');
}
