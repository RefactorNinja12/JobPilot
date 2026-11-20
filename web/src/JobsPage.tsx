import { useEffect, useState } from 'react'
import { ApiError, fetchAssessedJobs, setInterest, type AssessedJob } from './api/jobs'
import { daysLeft, deadlineText } from './deadline'
import { ApplicationSupportPanel } from './ApplicationSupportPanel'
import { JobDescription } from './JobDescription'

/** Claude gives a chance, not a yes or no: from this chance up, an ad counts as worth applying for. */
const WORTH_APPLYING_FROM = 50

type Tab = 'wanted' | 'good' | 'poor'

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

function SkillRow({ label, skills, kind }: { label: string; skills: string[]; kind: 'matched' | 'missing' }) {
  if (skills.length === 0) return null
  return (
    <div className="skill-row">
      <span className="skill-row-label">{label}</span>
      <ul className="skills" aria-label={label}>
        {skills.map((skill) => (
          <li key={skill} className={`skill skill-${kind}`}>
            {skill}
          </li>
        ))}
      </ul>
    </div>
  )
}

function JobCard({
  job,
  interested,
  onToggleInterest,
}: {
  job: AssessedJob
  interested: boolean
  onToggleInterest: (wanted: boolean) => Promise<void>
}) {
  const days = daysLeft(job.applicationDeadline)
  const [error, setError] = useState<string>()

  const toggle = () => {
    setError(undefined)
    onToggleInterest(!interested).catch((e: unknown) => setError(errorText(e)))
  }

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
        <SkillRow label="Du har" skills={job.matchedSkills} kind="matched" />
        <SkillRow label="Du saknar" skills={job.missingSkills} kind="missing" />
        <p className="assessment">
          <strong>Claudes bedömning:</strong> {job.summary}
        </p>
        <div className="job-actions">
          <button type="button" className={interested ? 'interest interest-on' : 'interest'} aria-pressed={interested} onClick={toggle}>
            {interested ? '✓ Du vill söka (ångra)' : 'Jag vill söka'}
          </button>
        </div>
        {error && (
          <p role="alert" className="error">
            {error}
          </p>
        )}
        {interested &&
          (job.hasSupport ? (
            <ApplicationSupportPanel adId={job.adId} />
          ) : (
            <p className="muted">
              Inget underlag än. Claude tar fram det automatiskt när bevakaren på din dator körs, annars kan du be Claude i
              chatten ta fram underlag för dina markerade annonser. Ladda om sidan när det är klart.
            </p>
          ))}
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

  return <AssessedJobs jobs={jobs} />
}

function AssessedJobs({ jobs }: { jobs: AssessedJob[] }) {
  const [tab, setTab] = useState<Tab>('good')
  const [wanted, setWanted] = useState(() => new Set(jobs.filter((j) => j.interested).map((j) => j.adId)))
  const good = jobs.filter((j) => j.chance >= WORTH_APPLYING_FROM)
  const poor = jobs.filter((j) => j.chance < WORTH_APPLYING_FROM)
  const marked = jobs.filter((j) => wanted.has(j.adId))
  const shown = tab === 'wanted' ? marked : tab === 'good' ? good : poor

  // The button answers at once; if the API says no, the mark goes back and the card shows why.
  const toggleInterest = async (adId: string, interest: boolean) => {
    const change = (on: boolean) =>
      setWanted((current) => {
        const next = new Set(current)
        if (on) next.add(adId)
        else next.delete(adId)
        return next
      })
    change(interest)
    try {
      await setInterest(adId, interest)
    } catch (e) {
      change(!interest)
      throw e
    }
  }

  return (
    <section>
      <div role="tablist" aria-label="Claudes bedömning" className="tabs">
        <button type="button" role="tab" aria-selected={tab === 'wanted'} onClick={() => setTab('wanted')}>
          Vill söka ({marked.length})
        </button>
        <button type="button" role="tab" aria-selected={tab === 'good'} onClick={() => setTab('good')}>
          Värda att söka ({good.length})
        </button>
        <button type="button" role="tab" aria-selected={tab === 'poor'} onClick={() => setTab('poor')}>
          Bortsållade ({poor.length})
        </button>
      </div>
      <p className="summary">
        {tab === 'wanted'
          ? 'Annonserna du har sagt att du vill söka. Claude tar fram underlag för just dem.'
          : tab === 'good'
            ? `Claude bedömer minst ${WORTH_APPLYING_FROM} % chans. Bäst chans först.`
            : `Under ${WORTH_APPLYING_FROM} % chans enligt Claude. Titta här om du vill se hur Claude har sållat.`}
      </p>
      {shown.length === 0 ? (
        <p>
          {tab === 'wanted'
            ? 'Du har inte markerat någon annons än. Tryck på "Jag vill söka" på en annons du vill gå vidare med.'
            : tab === 'good'
              ? 'Inga annonser med god chans just nu.'
              : 'Claude har inte sållat bort någon annons.'}
        </p>
      ) : (
        <div role="tabpanel">
          {shown.map((job) => (
            <JobCard
              key={job.adId}
              job={job}
              interested={wanted.has(job.adId)}
              onToggleInterest={(on) => toggleInterest(job.adId, on)}
            />
          ))}
        </div>
      )}
    </section>
  )
}
