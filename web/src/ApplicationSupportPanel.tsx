import { useEffect, useState } from 'react'
import { ApiError, fetchApplicationSupport, type ApplicationSupport } from './api/jobs'

/** A button that copies a text to the clipboard and says so, or says it could not. */
function CopyButton({ text, label }: { text: string; label: string }) {
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle')

  useEffect(() => {
    if (state === 'idle') return
    const timer = setTimeout(() => setState('idle'), 2000)
    return () => clearTimeout(timer)
  }, [state])

  const copy = () => {
    navigator.clipboard.writeText(text).then(
      () => setState('copied'),
      () => setState('failed'),
    )
  }

  return (
    <button type="button" className="copy" onClick={copy} aria-label={`Kopiera ${label}`}>
      {state === 'copied' ? 'Kopierat ✓' : state === 'failed' ? 'Kunde inte kopiera' : 'Kopiera'}
    </button>
  )
}

function Section({ title, text }: { title: string; text: string | null }) {
  if (!text) return null
  return (
    <section className="support-section" aria-label={title}>
      <h3>{title}</h3>
      <p className="support-text">{text}</p>
      <CopyButton text={text} label={title.toLowerCase()} />
    </section>
  )
}

function Support({ support }: { support: ApplicationSupport }) {
  return (
    <div className="support">
      <section className="support-section" aria-label="Nyckelord">
        <h3>Nyckelord att ha med</h3>
        <ul className="skills" aria-label="Nyckelord">
          {support.keywords.map((keyword) => (
            <li key={keyword} className="skill skill-matched">
              {keyword}
            </li>
          ))}
        </ul>
        <CopyButton text={support.keywords.join(', ')} label="nyckelord" />
      </section>
      <Section title="Det här lyfter du" text={support.highlights} />
      <Section title="Förslag på CV-ändringar" text={support.cvSuggestions} />
      <Section title="Utkast till brev" text={support.letterDraft} />
      <Section title="Om företaget" text={support.companyNotes} />
    </div>
  )
}

/**
 * What Claude prepared for the application to one ad: fetched the first time it is opened, then toggled. Each part has a
 * copy button, since the user pastes it into their own CV and letter.
 */
export function ApplicationSupportPanel({ adId }: { adId: string }) {
  const [open, setOpen] = useState(false)
  const [support, setSupport] = useState<ApplicationSupport>()
  const [error, setError] = useState<string>()

  function toggle() {
    const opening = !open
    setOpen(opening)
    if (opening && !support) {
      setError(undefined)
      fetchApplicationSupport(adId)
        .then(setSupport)
        .catch((e: unknown) =>
          setError(
            e instanceof ApiError && e.code === 'ApplicationSupport.NotFound'
              ? 'Inget underlag hittades för annonsen.'
              : 'Kunde inte hämta underlaget. Försök igen.',
          ),
        )
    }
  }

  const panelId = `support-${adId}`
  return (
    <>
      <button type="button" className="toggle" aria-expanded={open} aria-controls={panelId} onClick={toggle}>
        {open ? 'Dölj underlag för ansökan' : 'Visa underlag för ansökan'}
      </button>
      {open && (
        <div id={panelId}>
          {error && <p role="alert" className="error">{error}</p>}
          {!error && !support && <p aria-busy="true">Hämtar underlaget…</p>}
          {support && <Support support={support} />}
        </div>
      )}
    </>
  )
}
