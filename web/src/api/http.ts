// Shared fetch helper: JSON in, ProblemDetails errors out as ApiError (with the backend's error code).

/** A failed API call, carrying the backend's error code (e.g. "FindJobs.NoOccupations") from ProblemDetails. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined

  constructor(status: number, code: string | undefined, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

export function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  return sendJson<T>('GET', url, undefined, signal)
}

/** Sends a request with an optional JSON body and reads the JSON answer. */
export async function sendJson<T>(method: string, url: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, {
    method,
    signal,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as { code?: string; detail?: string }
    throw new ApiError(response.status, problem.code, problem.detail ?? `HTTP ${response.status}`)
  }

  return (await response.json()) as T
}
