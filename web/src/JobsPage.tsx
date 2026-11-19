import { useEffect, useState } from 'react'
import { ApiError, fetchAssessedJobs, type AssessedJob } from './api/jobs'
import { daysLeft, deadlineText } from './deadline'
import { JobDescription } from './JobDescription'

const date = (iso: string) => new Date(iso).toLocaleDateString('sv-SE', { day: 'numeric', month: 'short' })

function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 503) {
      return `En tjänst svarar inte just nu: ${error.message} Försök igen om en stund.`
    }
    return error.message
  }
  return 'Kunde inte nå Jobpilot. Kör API:t (dotnet run --project src/Jobpilot.Api)?'
}

function chanceClass(chance: number) {
  if (chance >= 70) return 'score score-high'
  if (chance >= 40) return 'score score-mid'
  return 'score score-low'
}

function JobCard({ job }: { job: AssessedJob }) {
  const days = daysLeft(job.applicationDeadline)
  return (
    <article className="job">
      <div className={chanceClass(job.chance)} title="Claudes bedömning av chansen att få jobbet">
        {job.chance} %
      </div>
      <div className="job-body">
        <h2>
          <a href={job.url} target="_blank" rel="noopener noreferrer">
            {job.headline}
          </a>
        </h2>
        <p className="meta">
          {job.employer} · {job.municipality ?? 'Ort saknas'} · Sista ansökan {date(job.applicationDeadline)}
        </p>
        <p className={days <= 3 ? 'deadline deadline-soon' : 'deadline'}>{deadlineText(job.applicationDeadline)}</p>
        <p className="assessment">
          <strong>Claudes bedömning:</strong> {job.summary}
        </p>
        <JobDescription adId={job.adId} />
      </div>
    </article>
  )
}

export function JobsPage() {
  const [jobs, setJobs] = useState<AssessedJob[]>()
  const [error, setError] = useState<unknown>()

  useEffect(() => {
    const controller = new AbortController()
    fetchAssessedJobs(controller.signal)
      .then(setJobs)
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(e)
      })
    return () => controller.abort()
  }, [])

  if (error) return <p role="alert" className="error">{errorText(error)}</p>
  if (!jobs) return <p aria-busy="true">Hämtar bedömda jobb…</p>

  if (jobs.length === 0) {
    return (
      <p>
        Inga bedömda annonser än. Be Claude köra find_jobs och spara sina bedömningar (save_assessment), så visas de
        här.
      </p>
    )
  }

  return (
    <section>
      <p className="summary">
        {jobs.length} annonser bedömda av Claude, bäst chans först. De med låg chans visas också, så att du kan se hur
        Claude har bedömt.
      </p>
      {jobs.map((job) => (
        <JobCard key={job.adId} job={job} />
      ))}
    </section>
  )
}
