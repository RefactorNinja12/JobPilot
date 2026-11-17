// Mirrors the JSON from GET /api/jobs (Jobpilot.Api). Read-only: the web UI never calls an AI.

export type JobAssessment = {
  chance: number
  summary: string
  assessedAt: string
}

export type JobMatch = {
  adId: string
  headline: string
  employer: string
  municipality: string | null
  occupation: string
  workingHours: 'FullTime' | 'PartTime' | 'Unknown'
  publishedAt: string
  applicationDeadline: string
  url: string
  fitScore: number
  matchedSkills: string[]
  missingSkills: string[]
  unknowns: string[]
  assessment: JobAssessment | null
}

export type FindJobsResult = {
  matches: JobMatch[]
  fetched: number
  skipped: number
  rejected: Record<string, number>
  passed: number
  newlyAnalysed: number
}

/** One ad in full, from GET /api/jobs/{adId}. */
export type JobAd = {
  adId: string
  headline: string
  employer: string
  municipality: string | null
  occupation: string
  workingHours: JobMatch['workingHours']
  publishedAt: string
  applicationDeadline: string
  url: string
  description: string
}

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

async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as { code?: string; detail?: string }
    throw new ApiError(response.status, problem.code, problem.detail ?? `HTTP ${response.status}`)
  }

  return (await response.json()) as T
}

export function fetchJobs(top = 25, signal?: AbortSignal): Promise<FindJobsResult> {
  return getJson<FindJobsResult>(`/api/jobs?top=${top}`, signal)
}

export function fetchJob(adId: string, signal?: AbortSignal): Promise<JobAd> {
  return getJson<JobAd>(`/api/jobs/${encodeURIComponent(adId)}`, signal)
}
