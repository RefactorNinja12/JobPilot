import { useState } from 'react'
import { ApiError, fetchJob, type JobAd } from './api/jobs'

/**
 * "Läs annonsen": fetches the full ad the first time it is opened, then toggles it.
 * Reading it here costs no tokens; only Claude's get_job_ad is limited.
 */
export function JobDescription({ adId }: { adId: string }) {
  const [open, setOpen] = useState(false)
  const [ad, setAd] = useState<JobAd>()
  const [error, setError] = useState<string>()

  function toggle() {
    const opening = !open
    setOpen(opening)
    if (opening && !ad) {
      setError(undefined)
      fetchJob(adId)
        .then(setAd)
        .catch((e: unknown) =>
          setError(
            e instanceof ApiError && e.code === 'JobSearch.AdNotFound'
              ? 'Annonsen finns inte längre i Platsbanken.'
              : 'Kunde inte hämta annonsen. Försök igen.',
          ),
        )
    }
  }

  const panelId = `ad-${adId}`
  return (
    <>
      <button type="button" className="toggle" aria-expanded={open} aria-controls={panelId} onClick={toggle}>
        {open ? 'Dölj annonsen' : 'Läs annonsen'}
      </button>
      {open && (
        <div id={panelId} className="description">
          {error && <p role="alert" className="error">{error}</p>}
          {!error && !ad && <p aria-busy="true">Hämtar annonsen…</p>}
          {ad && <p>{ad.description}</p>}
        </div>
      )}
    </>
  )
}
