/** The one error the clients' data layers throw. Status 0 = transport failure/timeout (no HTTP response). */
export class ApiError extends Error {
  readonly status: number;
  readonly title?: string;
  readonly traceId?: string;

  constructor(status: number, message: string, problem?: { title?: string; traceId?: string }) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.title = problem?.title;
    this.traceId = problem?.traceId;
  }

  /** From an error response body: problem+json `title`/`traceId` when present; the message per `problemMessage`. */
  static fromBody(status: number, body: string, fallback: string): ApiError {
    const { title, traceId } = parseProblem(body);
    return new ApiError(status, problemMessage(body, fallback), { title, traceId });
  }
}

export function isNetworkError(e: unknown): boolean {
  return e instanceof ApiError && e.status === 0;
}

/** The human line of an error response: a problem+json document's `detail` or `title` (the APIs answer
 *  400/403/409 that way), else the raw body, else `fallback`. */
export function problemMessage(body: string, fallback: string): string {
  const { detail, title } = parseProblem(body);
  return detail ?? title ?? (body || fallback);
}

function parseProblem(body: string): { detail?: string; title?: string; traceId?: string } {
  let problem: unknown;
  try {
    problem = JSON.parse(body);
  } catch {
    return {};
  }
  if (typeof problem !== 'object' || problem === null) return {};
  const field = (name: string) => {
    const value = (problem as Record<string, unknown>)[name];
    return typeof value === 'string' && value ? value : undefined;
  };
  return { detail: field('detail'), title: field('title'), traceId: field('traceId') };
}

/** Shown when a request never reached the server. */
export const NETWORK_ERROR = 'Network error — check your connection and try again.';

/** What to show a person for a caught error — its message, without the "Error:" prefix String() adds. */
export function errorText(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}
