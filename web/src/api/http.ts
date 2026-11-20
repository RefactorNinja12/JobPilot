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

export function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  return sendJson<T>('GET', path, undefined, signal)
}

/**
 * Resolves a path against the page and refuses anything that is not this site's own API, so no input (a skill
 * name, an ad id) can make the UI call another server.
 */
export function apiUrl(path: string): URL {
  const url = new URL(path, window.location.origin)
  if (url.origin !== window.location.origin || !url.pathname.startsWith('/api/')) {
    throw new Error(`Refusing to call ${path}: only this site's /api/ is allowed.`)
  }
  return url
}

/** Sends a request with an optional JSON body and reads the JSON answer. */
export async function sendJson<T>(method: string, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const response = await fetch(apiUrl(path), {
    method,
    signal,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  return readJson<T>(response)
}

/** Uploads one file as multipart form data (field "file") and reads the JSON answer. */
export async function postFile<T>(path: string, file: File, signal?: AbortSignal): Promise<T> {
  const form = new FormData()
  form.append('file', file)
  return readJson<T>(await fetch(apiUrl(path), { method: 'POST', body: form, signal }))
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as { code?: string; detail?: string }
    throw new ApiError(response.status, problem.code, problem.detail ?? `HTTP ${response.status}`)
  }

  return (await response.json()) as T
}
