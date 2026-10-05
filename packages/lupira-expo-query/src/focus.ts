import { focusManager } from '@tanstack/react-query';
import { AppState } from 'react-native';

/** Call once at startup: stale queries refetch when the app returns to the foreground. */
export function connectFocusManager(): void {
  focusManager.setEventListener((setFocused) => {
    const sub = AppState.addEventListener('change', (state) => setFocused(state === 'active'));
    return () => sub.remove();
  });
}
