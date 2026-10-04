import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DEFAULT_DISMISS_MS, toast, toastError, useToast } from './toast.ts';

const { hapticError } = vi.hoisted(() => ({ hapticError: vi.fn() }));
vi.mock('./haptics.ts', () => ({ hapticError }));

describe('toast', () => {
  beforeEach(() => {
    useToast.setState({ message: null, action: null, durationMs: DEFAULT_DISMISS_MS, nonce: 0, clearance: 0 });
    hapticError.mockClear();
  });

  it('shows the message with the default duration and no action', () => {
    toast('Saved');
    expect(useToast.getState()).toMatchObject({ message: 'Saved', action: null, durationMs: DEFAULT_DISMISS_MS, nonce: 1 });
  });

  it('bumps the nonce on an identical repeat so the host re-arms its timer', () => {
    toast('Saved');
    toast('Saved');
    expect(useToast.getState().nonce).toBe(2);
  });

  it('replaces the current toast, keeping its own action and duration', () => {
    const onPress = () => {};
    toast('Deleted', { action: { label: 'Undo', onPress }, durationMs: 6000 });
    expect(useToast.getState()).toMatchObject({ message: 'Deleted', action: { label: 'Undo', onPress }, durationMs: 6000 });
    toast('Saved');
    expect(useToast.getState()).toMatchObject({ message: 'Saved', action: null, durationMs: DEFAULT_DISMISS_MS });
  });

  it('hide clears message and action but keeps the clearance', () => {
    useToast.getState().setClearance(64);
    toast('Saved', { action: { label: 'Undo', onPress: () => {} } });
    useToast.getState().hide();
    expect(useToast.getState()).toMatchObject({ message: null, action: null, clearance: 64 });
  });

  it('toastError fires the error haptic', () => {
    toastError('Could not save.');
    expect(hapticError).toHaveBeenCalledOnce();
    expect(useToast.getState().message).toBe('Could not save.');
  });
});
