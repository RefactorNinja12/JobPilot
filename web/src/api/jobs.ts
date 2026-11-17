// Mirrors the JSON from GET /api/jobs (Jobpilot.Api). Read-only: the web UI never calls an AI.
import { getJson } from './http'

export { ApiError } from './http'

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

export function fetchJobs(top = 25, signal?: AbortSignal): Promise<FindJobsResult> {
  return getJson<FindJobsResult>(`/api/jobs?top=${top}`, signal)
}

export function fetchJob(adId: string, signal?: AbortSignal): Promise<JobAd> {
  return getJson<JobAd>(`/api/jobs/${encodeURIComponent(adId)}`, signal)
}
