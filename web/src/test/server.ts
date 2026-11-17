import { http, HttpResponse, type JsonBodyType } from 'msw'
import { setupServer } from 'msw/node'
import type { FindJobsResult, JobMatch } from '../api/jobs'

/** A match as GET /api/jobs returns it; override what a test cares about. */
export function aMatch(overrides: Partial<JobMatch> = {}): JobMatch {
  return {
    adId: '31744367',
    headline: 'Backendutvecklare C#',
    employer: 'Exempel AB',
    municipality: 'Göteborg',
    occupation: 'Backend-utvecklare',
    workingHours: 'FullTime',
    publishedAt: '2026-11-16T09:00:00+01:00',
    applicationDeadline: '2026-12-01',
    url: 'https://arbetsformedlingen.se/platsbanken/annonser/31744367',
    fitScore: 85,
    matchedSkills: ['C#', '.net'],
    missingSkills: ['Kafka'],
    unknowns: [],
    assessment: null,
    ...overrides,
  }
}

export function aResult(overrides: Partial<FindJobsResult> = {}): FindJobsResult {
  return { matches: [aMatch()], fetched: 200, skipped: 0, rejected: {}, passed: 150, newlyAnalysed: 0, ...overrides }
}

/** Answers GET /api/jobs with the given body and status, like Jobpilot.Api. */
export function respondWithJobs(body: JsonBodyType, status = 200) {
  server.use(http.get('/api/jobs', () => HttpResponse.json(body, { status })))
}

/** Answers GET /api/jobs/{adId} with the given body and status, like Jobpilot.Api. Returns the requests seen. */
export function respondWithJob(body: JsonBodyType, status = 200) {
  const requests: string[] = []
  server.use(
    http.get('/api/jobs/:adId', ({ params }) => {
      requests.push(String(params.adId))
      return HttpResponse.json(body, { status })
    }),
  )
  return requests
}

export const server = setupServer()
