// Mirrors the JSON from Jobpilot.Api. Read-only: the web UI never calls an AI or searches JobTech for its list.
import { getJson, sendJson } from './http'

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
  /** What the ad asks for that the profile had when Claude assessed it. Empty when not known (older assessments). */
  matchedSkills: string[]
  /** What the ad asks for that the profile lacked when Claude assessed it. Empty when not known. */
  missingSkills: string[]
  /** True when the user has said they want to apply for the ad. */
  interested: boolean
  /** True when Claude has prepared application support (keywords, what to lift, drafts) for the ad. */
  hasSupport: boolean
}

/** What Claude prepared for the user's application to one ad, from GET /api/jobs/{adId}/support. */
export type ApplicationSupport = {
  adId: string
  /** Words from the ad worth using in the CV and letter, most important first. */
  keywords: string[]
  /** What to lift from the profile and the base CV, and honest gaps. */
  highlights: string
  cvSuggestions: string | null
  letterDraft: string | null
  companyNotes: string | null
  updatedAt: string
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

/** Marks the ad as one the user wants to apply for, or takes the mark away. */
export function setInterest(adId: string, interested: boolean): Promise<{ adId: string; interested: boolean }> {
  return sendJson('PUT', `/api/jobs/${encodeURIComponent(adId)}/interest`, { interested })
}

export function fetchApplicationSupport(adId: string, signal?: AbortSignal): Promise<ApplicationSupport> {
  return getJson<ApplicationSupport>(`/api/jobs/${encodeURIComponent(adId)}/support`, signal)
}

export function fetchJob(adId: string, signal?: AbortSignal): Promise<JobAd> {
  return getJson<JobAd>(`/api/jobs/${encodeURIComponent(adId)}`, signal)
}
