import { useCallback, useEffect, useState } from 'react'
import { fetchProfile, type Profile } from './api/profile'
import { OccupationsEditor } from './OccupationsEditor'
import { PreferencesEditor } from './PreferencesEditor'
import { SkillsEditor } from './SkillsEditor'

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
        <OccupationsEditor occupations={profile.targetOccupations} onChanged={() => load()} />
      </section>

      <section aria-labelledby="preferences-heading">
        <h2 id="preferences-heading">Dina krav</h2>
        <PreferencesEditor
          key={JSON.stringify(profile.preferences)}
          preferences={profile.preferences}
          onChanged={() => load()}
        />
      </section>
    </div>
  )
}
