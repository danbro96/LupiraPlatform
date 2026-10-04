/** The seam between the generated clients and each app's own HTTP concerns: the clients call through here
 *  and each app installs its own transport at startup. */
export type ApiTransport = <T>(url: string, init?: RequestInit) => Promise<T>;

let transport: ApiTransport | null = null;

/** Install the app's transport. Call once, before anything issues a request. */
export function setApiTransport(next: ApiTransport): void {
  transport = next;
}

/** Orval mutator. Paths already carry their BFF route prefix, so this prepends nothing. */
export function apiRequest<T>(url: string, init?: RequestInit): Promise<T> {
  if (!transport) {
    throw new Error('@danbro96/lupira-http: no transport installed — call setApiTransport() during startup.');
  }
  return transport<T>(url, init);
}
