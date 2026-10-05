export type Serial = <T>(task: () => Promise<T>) => Promise<T>;

/** Runs tasks one at a time in call order; a failed task does not block the next. */
export function createSerial(): Serial {
  let tail: Promise<unknown> = Promise.resolve();
  return (task) => {
    const run = tail.then(task);
    tail = run.catch(() => undefined);
    return run;
  };
}
