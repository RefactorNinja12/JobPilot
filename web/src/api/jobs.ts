// Mirrors the JSON from Jobpilot.Api. Read-only: the web UI never calls an AI or searches JobTech for its list.
import { getJson } from './http'

export { ApiError } from './http'

/** One ad Claude has assessed, from GET /api/jobs/assessed. Read from the database only. */
export type AssessedJob = {
  adId: string
  headline: string
  employer: string
  municipality: string | null
  url: string
  publishedAt: string
  applicationDeadline: string
  chance: number
  summary: string
  assessedAt: string
}

/** One ad in full, from GET /api/jobs/{adId}. */
export type JobAd = {
  adId: string
  headline: string
  employer: string
  municipality: string | null
  occupation: string
  workingHours: 'FullTime' | 'PartTime' | 'Unknown'
  publishedAt: string
  applicationDeadline: string
  url: string
  description: string
}

/** Every assessed ad, good and poor chances alike, best chance first. */
export function fetchAssessedJobs(signal?: AbortSignal): Promise<AssessedJob[]> {
  return getJson<AssessedJob[]>('/api/jobs/assessed', signal)
}

export function fetchJob(adId: string, signal?: AbortSignal): Promise<JobAd> {
  return getJson<JobAd>(`/api/jobs/${encodeURIComponent(adId)}`, signal)
}
