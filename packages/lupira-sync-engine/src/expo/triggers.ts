import { authPort } from '@danbro96/lupira-http/authPort';
import NetInfo from '@react-native-community/netinfo';
import * as BackgroundTask from 'expo-background-task';
import * as TaskManager from 'expo-task-manager';
import { AppState } from 'react-native';
import type { SyncEngine } from '../engine.ts';

type Syncable = Pick<SyncEngine, 'sync'>;

/** Call at module scope: the OS runs the task in a headless JS context where no component ever mounts, so
 *  `prepare` loads what the app's startup would have (the auth session) before syncing. */
export function defineSyncTask(taskName: string, engine: Syncable, prepare?: () => Promise<void>): void {
  TaskManager.defineTask(taskName, async () => {
    await prepare?.();
    await engine.sync();
    return BackgroundTask.BackgroundTaskResult.Success;
  });
}

export interface SyncTriggerOptions {
  backgroundTaskName: string;
  /** False in development builds: a job firing there creates React headlessly, which the dev launcher rejects. */
  registerBackgroundTask?: boolean;
}

/** Syncs now, on return to the foreground, on regained connectivity and on sign-in, and registers the 15-minute
 *  background task (or removes it when `registerBackgroundTask` is false). Returns the unsubscribe for the
 *  foreground triggers. */
export function startSyncTriggers(engine: Syncable, { backgroundTaskName, registerBackgroundTask = true }: SyncTriggerOptions): () => void {
  const appState = AppState.addEventListener('change', (state) => {
    if (state === 'active') void engine.sync();
  });
  const net = NetInfo.addEventListener((state) => {
    if (state.isConnected) void engine.sync();
  });
  const signIn = authPort().onSignIn(() => void engine.sync());
  // Unavailable in Expo Go; the foreground triggers still cover everything.
  const background = registerBackgroundTask
    ? BackgroundTask.registerTaskAsync(backgroundTaskName, { minimumInterval: 15 })
    : BackgroundTask.unregisterTaskAsync(backgroundTaskName);
  background.catch(() => undefined);
  void engine.sync();
  return () => {
    appState.remove();
    net();
    signIn();
  };
}
