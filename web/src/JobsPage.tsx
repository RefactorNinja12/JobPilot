import { useEffect, useState } from 'react'
import { ApiError, fetchJobs, type FindJobsResult, type JobMatch } from './api/jobs'
import { JobDescription } from './JobDescription'

const workingHoursText: Record<JobMatch['workingHours'], string> = {
  FullTime: 'Heltid',
  PartTime: 'Deltid',
  Unknown: 'Arbetstid okänd',
}

const rejectionText: Record<string, string> = {
  TooFarAway: 'för långt bort',
  FullTimeNotAccepted: 'heltid (vill inte)',
  PartTimeNotAccepted: 'deltid (vill inte)',
  StaffingAgency: 'bemanningsföretag',
}

const unknownText: Record<string, string> = {
  Location: 'avståndet (annonsen saknar plats)',
  WorkingHours: 'arbetstiden (annonsen säger inget)',
}

const date = (iso: string) => new Date(iso).toLocaleDateString('sv-SE', { day: 'numeric', month: 'short' })

function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.code === 'FindJobs.NoOccupations') {
      return 'Du har inga målyrken än. Be Claude välja yrken åt dig (search_occupations och set_occupations).'
    }
    if (error.status === 503) {
      return `En tjänst svarar inte just nu: ${error.message} Försök igen om en stund.`
    }
    return error.message
  }
  return 'Kunde inte nå Jobpilot. Kör API:t (dotnet run --project src/Jobpilot.Api)?'
}

function scoreClass(score: number) {
  if (score >= 80) return 'score score-high'
  if (score >= 50) return 'score score-mid'
  return 'score score-low'
}

function Summary({ result }: { result: FindJobsResult }) {
  const rejected = Object.entries(result.rejected)
    .map(([reason, count]) => `${count} ${rejectionText[reason] ?? reason}`)
    .join(', ')
  return (
    <p className="summary">
      {result.fetched} hämtade · {result.passed} klarade dina krav
      {rejected && ` · bortsorterade: ${rejected}`}
    </p>
  )
}

function JobCard({ job }: { job: JobMatch }) {
  return (
    <article className="job">
      <div className={scoreClass(job.fitScore)} title="Hur väl dina kompetenser täcker annonsens">
        {job.fitScore}
      </div>
      <div className="job-body">
        <h2>
          <a href={job.url} target="_blank" rel="noopener noreferrer">
            {job.headline}
          </a>
        </h2>
        <p className="meta">
          {job.employer} · {job.municipality ?? 'Ort saknas'} · {workingHoursText[job.workingHours]} · Sista ansökan{' '}
          {date(job.applicationDeadline)}
        </p>
        {job.matchedSkills.length > 0 && (
          <ul className="skills" aria-label="Matchade kompetenser">
            {job.matchedSkills.map((skill) => (
              <li key={skill} className="skill skill-matched">
                {skill}
              </li>
            ))}
          </ul>
        )}
        {job.missingSkills.length > 0 && (
          <ul className="skills" aria-label="Saknade kompetenser">
            {job.missingSkills.map((skill) => (
              <li key={skill} className="skill skill-missing">
                {skill}
              </li>
            ))}
          </ul>
        )}
        {job.assessment && (
          <p className="assessment">
            <strong>Claudes bedömning: {job.assessment.chance} % chans.</strong> {job.assessment.summary}
          </p>
        )}
        {job.unknowns.length > 0 && (
          <p className="unknowns">Kunde inte kontrollera {job.unknowns.map((u) => unknownText[u] ?? u).join(' och ')}.</p>
        )}
        <JobDescription adId={job.adId} />
      </div>
    </article>
  )
}

export function JobsPage() {
  const [result, setResult] = useState<FindJobsResult>()
  const [error, setError] = useState<unknown>()

  useEffect(() => {
    const controller = new AbortController()
    fetchJobs(25, controller.signal)
      .then(setResult)
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(e)
      })
    return () => controller.abort()
  }, [])

  if (error) return <p role="alert" className="error">{errorText(error)}</p>
  if (!result) return <p aria-busy="true">Hämtar jobb…</p>

  return (
    <section>
      <Summary result={result} />
      {result.matches.length === 0 ? (
        <p>Inga annonser klarade dina krav just nu.</p>
      ) : (
        result.matches.map((job) => <JobCard key={job.adId} job={job} />)
      )}
    </section>
  )
}
