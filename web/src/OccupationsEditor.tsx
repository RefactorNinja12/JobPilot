import { useEffect, useState } from 'react'
import { ApiError } from './api/http'
import { searchOccupations, setOccupations, type Occupation } from './api/profile'

/** The profile allows at most this many (Profile.MaxTargetOccupations). */
const maxOccupations = 10

const message = (e: unknown) => (e instanceof ApiError ? e.message : 'Något gick fel. Försök igen.')

/** Choose the Taxonomy occupations job ads are searched for, as Claude does with set_occupations. */
export function OccupationsEditor({ occupations, onChanged }: { occupations: Occupation[]; onChanged: () => void }) {
  const [text, setText] = useState('')
  const [suggestions, setSuggestions] = useState<Occupation[]>([])
  const [error, setError] = useState<string>()

  // Suggestions while typing, after a short pause.
  useEffect(() => {
    if (text.trim().length < 2) return
    const controller = new AbortController()
    const timer = setTimeout(() => {
      searchOccupations(text, controller.signal).then(setSuggestions, () => setSuggestions([]))
    }, 250)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [text])

  const save = (next: Occupation[]) => {
    setError(undefined)
    setOccupations(next).then(onChanged, (e: unknown) => setError(message(e)))
  }

  const add = (occupation: Occupation) => {
    setText('')
    setSuggestions([])
    save([...occupations, occupation])
  }

  const remove = (taxonomyId: string) => save(occupations.filter((o) => o.taxonomyId !== taxonomyId))

  const full = occupations.length >= maxOccupations
  const chosen = new Set(occupations.map((o) => o.taxonomyId))
  const visibleSuggestions = text.trim().length < 2 ? [] : suggestions.filter((s) => !chosen.has(s.taxonomyId))

  return (
    <>
      {occupations.length === 0 ? (
        <p className="muted">Inga yrken valda. Sök och lägg till dem du vill ha annonser för.</p>
      ) : (
        <ul className="skills" aria-label="Valda yrken">
          {occupations.map((o) => (
            <li key={o.taxonomyId} className="skill skill-matched occupation">
              {o.label}
              <button type="button" className="remove" aria-label={`Ta bort ${o.label}`} onClick={() => remove(o.taxonomyId)}>
                ×
              </button>
            </li>
          ))}
        </ul>
      )}

      {full ? (
        <p className="muted">Du har valt {maxOccupations} yrken, vilket är max. Ta bort ett för att lägga till ett annat.</p>
      ) : (
        <input
          className="occupation-search"
          aria-label="Sök yrke"
          placeholder="t.ex. backend eller programmerare"
          value={text}
          onChange={(e) => setText(e.target.value)}
        />
      )}
      {!full && visibleSuggestions.length > 0 && (
        <ul className="suggestions" aria-label="Yrkesförslag">
          {visibleSuggestions.map((s) => (
            <li key={s.taxonomyId}>
              <button type="button" onClick={() => add(s)}>
                + {s.label}
              </button>
            </li>
          ))}
        </ul>
      )}
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
    </>
  )
}
