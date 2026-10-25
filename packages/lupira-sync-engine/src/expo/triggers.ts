import { authPort } from '@danbro96/lupira-http/authPort';
import NetInfo from '@react-native-community/netinfo';
import * as BackgroundTask from 'expo-background-task';
import * as TaskManager from 'expo-task-manager';
import { AppState } from 'react-native';
import type { SyncEngine } from '../engine.ts';

type Syncable = Pick<SyncEngine, 'sync'>;

/** Call at module scope: the OS runs the task in a headless JS context where no component ever mounts, so
 *  `prepare` loads what the app's startup would have (the auth session) before syncing; false skips the sync. */
export function defineSyncTask(taskName: string, engine: Syncable, prepare?: () => Promise<boolean | void>): void {
  TaskManager.defineTask(taskName, async () => {
    if ((await prepare?.()) === false) return BackgroundTask.BackgroundTaskResult.Success;
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
 *  background task (or removes it when `registerBackgroundTask` is false); sign-in registers it again and sign-out
 *  removes it. Returns the unsubscribe for the foreground and session triggers. */
export function startSyncTriggers(engine: Syncable, { backgroundTaskName, registerBackgroundTask = true }: SyncTriggerOptions): () => void {
  // eslint-disable-next-line @typescript-eslint/no-floating-promises -- sync() never rejects; failures land in its status.
  const syncNow = () => void engine.sync();
  const appState = AppState.addEventListener('change', (state) => {
    if (state === 'active') syncNow();
  });
  const net = NetInfo.addEventListener((state) => {
    if (state.isConnected) syncNow();
  });
  // Unavailable in Expo Go; the foreground triggers still cover everything.
  const background = (register: boolean) =>
    void (register
      ? BackgroundTask.registerTaskAsync(backgroundTaskName, { minimumInterval: 15 })
      : BackgroundTask.unregisterTaskAsync(backgroundTaskName)
    ).catch(() => undefined);
  const signIn = authPort().onSignIn(() => {
    background(registerBackgroundTask);
    syncNow();
  });
  const signOut = authPort().onSignOut(() => background(false));
  background(registerBackgroundTask);
  syncNow();
  return () => {
    appState.remove();
    net();
    signIn();
    signOut();
  };
}
