import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as Sentry from '@sentry/react-native';
import { detach, logDebug, redact, useDebugLog } from './log.ts';

vi.mock('@sentry/react-native', () => ({ addBreadcrumb: vi.fn(), captureException: vi.fn() }));

const globals = globalThis as { __DEV__?: boolean };

beforeEach(() => {
  globals.__DEV__ = false;
  useDebugLog.getState().clear();
  vi.mocked(Sentry.addBreadcrumb).mockClear();
  vi.mocked(Sentry.captureException).mockClear();
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('redact', () => {
  const key = `${'a'.repeat(32)}.${'b'.repeat(64)}`;

  it('masks a bare device key', () => {
    expect(redact(`posted with ${key} ok`)).toBe('posted with [redacted-key] ok');
  });

  it('masks a DeviceKey authorization header', () => {
    expect(redact(`Authorization: DeviceKey ${key}`)).toBe('Authorization: DeviceKey [redacted]');
  });

  it('leaves ordinary text alone', () => {
    expect(redact('sync 200 in 41ms')).toBe('sync 200 in 41ms');
  });
});

describe('logDebug', () => {
  it('skips the console outside dev but keeps the buffer and breadcrumb', () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => {});
    logDebug('sync', 'ok');
    expect(log).not.toHaveBeenCalled();
    expect(useDebugLog.getState().entries).toHaveLength(1);
    expect(Sentry.addBreadcrumb).toHaveBeenCalledWith(expect.objectContaining({ category: 'debug', message: 'sync' }));
  });

  it('logs to the console in dev', () => {
    globals.__DEV__ = true;
    const log = vi.spyOn(console, 'log').mockImplementation(() => {});
    logDebug('sync', 'ok');
    expect(log).toHaveBeenCalledWith('[debug]', 'sync', 'ok');
  });
});

describe('detach', () => {
  it('captures a rejection tagged with the stage', async () => {
    const error = new Error('boom');
    const work = Promise.reject(error);
    detach(work, 'photos.upload');
    await work.catch(() => {});
    await Promise.resolve();
    expect(Sentry.captureException).toHaveBeenCalledWith(error, { tags: { area: 'photos.upload' } });
    expect(useDebugLog.getState().entries.at(-1)).toMatchObject({ stage: 'photos.upload', detail: 'failed: Error: boom' });
  });

  it('captures a repeated failure once per stage', async () => {
    const fail = (stage: string) => {
      const work = Promise.reject(new Error('denied'));
      detach(work, stage);
      return work.catch(() => {});
    };
    await fail('photos.scan');
    await fail('photos.scan');
    await fail('maps.upload');
    await Promise.resolve();
    expect(Sentry.captureException).toHaveBeenCalledTimes(2);
    expect(useDebugLog.getState().entries).toHaveLength(3);
  });

  it('captures nothing when the work resolves', async () => {
    const work = Promise.resolve(1);
    detach(work, 'photos.upload');
    await work;
    await Promise.resolve();
    expect(Sentry.captureException).not.toHaveBeenCalled();
  });
});
