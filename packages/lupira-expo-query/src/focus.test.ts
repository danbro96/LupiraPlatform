import { focusManager } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

const listeners: Array<(state: string) => void> = [];
vi.mock('react-native', () => ({
  AppState: { addEventListener: (_: string, fn: (state: string) => void) => (listeners.push(fn), { remove: vi.fn() }) },
}));

const { connectFocusManager } = await import('./focus.ts');

describe('connectFocusManager', () => {
  it('follows the app state', () => {
    connectFocusManager();
    listeners.at(-1)!('background');
    expect(focusManager.isFocused()).toBe(false);
    listeners.at(-1)!('active');
    expect(focusManager.isFocused()).toBe(true);
  });
});
