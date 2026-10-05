import type { QueryClient } from '@tanstack/react-query';

/** A sync engine `onChange` handler: refetches the changed aggregate's queries plus the roots derived from it,
 *  e.g. `{ contact: ['occurrences'] }` for birthdays shown in the calendar. */
export function invalidateOnChange(
  queryClient: QueryClient,
  derivedRoots: Record<string, string[]> = {},
): (event: { aggregate: string }) => void {
  return ({ aggregate }) => {
    for (const root of [aggregate, ...(derivedRoots[aggregate] ?? [])]) void queryClient.invalidateQueries({ queryKey: [root] });
  };
}
