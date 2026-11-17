import { useCallback, useEffect, useState } from 'react'
import { fetchProfile, type Preferences, type Profile } from './api/profile'
import { SkillsEditor } from './SkillsEditor'

function workingHours(p: Preferences) {
  if (p.acceptsFullTime && p.acceptsPartTime) return 'Heltid och deltid'
  return p.acceptsFullTime ? 'Bara heltid' : 'Bara deltid'
}

function commute(p: Preferences) {
  if (!p.commute) return 'Ingen gräns'
  const { maxDistanceKm, homeLatitude, homeLongitude } = p.commute
  return `Högst ${maxDistanceKm} km fågelvägen från hemmet (${homeLatitude.toFixed(2)}, ${homeLongitude.toFixed(2)})`
}

export function ProfilePage() {
  const [profile, setProfile] = useState<Profile>()
  const [failed, setFailed] = useState(false)

  const load = useCallback((signal?: AbortSignal) => {
    fetchProfile(signal)
      .then(setProfile)
      .catch(() => {
        if (!signal?.aborted) setFailed(true)
      })
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    load(controller.signal)
    return () => controller.abort()
  }, [load])

  if (failed) return <p role="alert" className="error">Kunde inte hämta profilen. Kör API:t?</p>
  if (!profile) return <p aria-busy="true">Hämtar profilen…</p>

  return (
    <div className="profile">
      <section aria-labelledby="skills-heading">
        <h2 id="skills-heading">Kompetenser</h2>
        <SkillsEditor skills={profile.skills} onChanged={() => load()} />
      </section>

      <section aria-labelledby="occupations-heading">
        <h2 id="occupations-heading">Yrken du söker</h2>
        {profile.targetOccupations.length === 0 ? (
          <p className="muted">Inga yrken valda. Be Claude välja dem (search_occupations och set_occupations).</p>
        ) : (
          <ul className="skills">
            {profile.targetOccupations.map((o) => (
              <li key={o.taxonomyId} className="skill skill-matched">
                {o.label}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="preferences-heading">
        <h2 id="preferences-heading">Dina krav</h2>
        <dl className="preferences">
          <dt>Arbetstid</dt>
          <dd>{workingHours(profile.preferences)}</dd>
          <dt>Bemanningsföretag</dt>
          <dd>{profile.preferences.excludeStaffingAgencies ? 'Visas inte' : 'Visas'}</dd>
          <dt>Pendling</dt>
          <dd>{commute(profile.preferences)}</dd>
        </dl>
      </section>
    </div>
  )
}
