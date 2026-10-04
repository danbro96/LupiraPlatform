import { describe, expect, it } from 'vitest';
import { nextAttemptDelayMs } from './backoff.ts';

const mid = () => 0.5;

describe('nextAttemptDelayMs', () => {
  it('doubles from 5 s per attempt', () => {
    expect([1, 2, 3, 4].map((a) => nextAttemptDelayMs(a, mid))).toEqual([5_000, 10_000, 20_000, 40_000]);
  });

  it('treats attempts below 1 as the first', () => {
    expect(nextAttemptDelayMs(0, mid)).toBe(5_000);
  });

  it('caps at 30 min', () => {
    expect(nextAttemptDelayMs(50, mid)).toBe(30 * 60_000);
  });

  it('jitters within ±20%', () => {
    expect(nextAttemptDelayMs(1, () => 0)).toBe(4_000);
    expect(nextAttemptDelayMs(1, () => 1)).toBe(6_000);
  });
});
