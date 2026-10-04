import { describe, expect, it, vi } from 'vitest';
import { redact } from './log.ts';

vi.mock('@sentry/react-native', () => ({ addBreadcrumb: vi.fn() }));

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
