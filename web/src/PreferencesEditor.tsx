import { useState, type FormEvent } from 'react'
import { ApiError } from './api/http'
import { setPreferences, type Preferences } from './api/profile'

const message = (e: unknown) => (e instanceof ApiError ? e.message : 'Något gick fel. Försök igen.')

/** Three decimals is about 100 m: plenty for a distance limit in km, and no more precise than it needs to be. */
const round = (degrees: number) => Math.round(degrees * 1000) / 1000

/** Form state: numbers as typed, so a field can be empty while editing. */
type Draft = {
  acceptsFullTime: boolean
  acceptsPartTime: boolean
  excludeStaffingAgencies: boolean
  limitCommute: boolean
  maxDistanceKm: string
  homeLatitude: string
  homeLongitude: string
}

function toDraft(p: Preferences): Draft {
  return {
    acceptsFullTime: p.acceptsFullTime,
    acceptsPartTime: p.acceptsPartTime,
    excludeStaffingAgencies: p.excludeStaffingAgencies,
    limitCommute: p.commute !== null,
    maxDistanceKm: String(p.commute?.maxDistanceKm ?? 30),
    homeLatitude: p.commute ? String(p.commute.homeLatitude) : '',
    homeLongitude: p.commute ? String(p.commute.homeLongitude) : '',
  }
}

function toPreferences(d: Draft): Preferences {
  return {
    acceptsFullTime: d.acceptsFullTime,
    acceptsPartTime: d.acceptsPartTime,
    excludeStaffingAgencies: d.excludeStaffingAgencies,
    commute: d.limitCommute
      ? { homeLatitude: Number(d.homeLatitude), homeLongitude: Number(d.homeLongitude), maxDistanceKm: Number(d.maxDistanceKm) }
      : null,
  }
}

/** Dealbreakers: ads that break them are never shown. Saved all at once, as Claude does with set_preferences. */
export function PreferencesEditor({ preferences, onChanged }: { preferences: Preferences; onChanged: () => void }) {
  const [draft, setDraft] = useState(() => toDraft(preferences))
  const [error, setError] = useState<string>()
  const [locating, setLocating] = useState(false)

  const update = (changes: Partial<Draft>) => setDraft((d) => ({ ...d, ...changes }))
  const changed = JSON.stringify(toPreferences(draft)) !== JSON.stringify(toPreferences(toDraft(preferences)))
  const noHours = !draft.acceptsFullTime && !draft.acceptsPartTime

  const useMyPosition = () => {
    setError(undefined)
    setLocating(true)
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLocating(false)
        update({ homeLatitude: String(round(position.coords.latitude)), homeLongitude: String(round(position.coords.longitude)) })
      },
      () => {
        setLocating(false)
        setError('Kunde inte hämta din position. Fyll i latitud och longitud själv.')
      },
    )
  }

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    setError(undefined)
    setPreferences(toPreferences(draft)).then(onChanged, (err: unknown) => setError(message(err)))
  }

  return (
    <form className="preferences-form" onSubmit={onSubmit} aria-label="Dina krav">
      <fieldset>
        <legend>Arbetstid</legend>
        <label>
          <input type="checkbox" checked={draft.acceptsFullTime} onChange={(e) => update({ acceptsFullTime: e.target.checked })} />
          Heltid
        </label>
        <label>
          <input type="checkbox" checked={draft.acceptsPartTime} onChange={(e) => update({ acceptsPartTime: e.target.checked })} />
          Deltid
        </label>
        {noHours && <p className="muted">Välj minst en, annars visas inga annonser.</p>}
      </fieldset>

      <label>
        <input
          type="checkbox"
          checked={draft.excludeStaffingAgencies}
          onChange={(e) => update({ excludeStaffingAgencies: e.target.checked })}
        />
        Dölj annonser från bemanningsföretag
      </label>

      <fieldset>
        <legend>Pendling</legend>
        <label>
          <input type="checkbox" checked={draft.limitCommute} onChange={(e) => update({ limitCommute: e.target.checked })} />
          Begränsa avståndet från hemmet
        </label>
        {draft.limitCommute && (
          <div className="commute">
            <label>
              Högst (km fågelvägen)
              <input
                type="number"
                min={1}
                max={500}
                required
                value={draft.maxDistanceKm}
                onChange={(e) => update({ maxDistanceKm: e.target.value })}
              />
            </label>
            <label>
              Latitud
              <input
                type="number"
                step="any"
                min={-90}
                max={90}
                required
                value={draft.homeLatitude}
                onChange={(e) => update({ homeLatitude: e.target.value })}
              />
            </label>
            <label>
              Longitud
              <input
                type="number"
                step="any"
                min={-180}
                max={180}
                required
                value={draft.homeLongitude}
                onChange={(e) => update({ homeLongitude: e.target.value })}
              />
            </label>
            {'geolocation' in navigator && (
              <button type="button" onClick={useMyPosition} disabled={locating}>
                {locating ? 'Hämtar position…' : 'Använd min position'}
              </button>
            )}
          </div>
        )}
      </fieldset>

      <div>
        <button type="submit" disabled={!changed || noHours}>
          Spara krav
        </button>
      </div>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
    </form>
  )
}
