import { useEffect, useState, type FormEvent } from 'react'
import { ApiError } from './api/http'
import { addSkill, removeSkill, searchSkills, updateSkill, type ProfileSkill, type SkillLevel, type SkillSuggestion } from './api/profile'

const levelText: Record<SkillLevel, string> = {
  Beginner: 'Nybörjare',
  Intermediate: 'Medel',
  Advanced: 'Avancerad',
  Expert: 'Expert',
}

const levels = Object.keys(levelText) as SkillLevel[]

const message = (e: unknown) => (e instanceof ApiError ? e.message : 'Något gick fel. Försök igen.')

function LevelSelect({ value, onChange, label }: { value: SkillLevel; onChange: (l: SkillLevel) => void; label: string }) {
  return (
    <select aria-label={label} value={value} onChange={(e) => onChange(e.target.value as SkillLevel)}>
      {levels.map((l) => (
        <option key={l} value={l}>
          {levelText[l]}
        </option>
      ))}
    </select>
  )
}

function SkillRow({ skill, onChanged, onError }: { skill: ProfileSkill; onChanged: () => void; onError: (m: string) => void }) {
  const [level, setLevel] = useState(skill.level)
  const [years, setYears] = useState(skill.yearsOfExperience)
  const changed = level !== skill.level || years !== skill.yearsOfExperience

  const save = () =>
    updateSkill({ name: skill.name, level, yearsOfExperience: years }).then(onChanged, (e: unknown) => onError(message(e)))

  const remove = () => {
    if (window.confirm(`Ta bort ${skill.name} från profilen?`)) {
      removeSkill(skill.name).then(onChanged, (e: unknown) => onError(message(e)))
    }
  }

  return (
    <tr>
      <td>{skill.name}</td>
      <td>
        <LevelSelect label={`Nivå för ${skill.name}`} value={level} onChange={setLevel} />
      </td>
      <td>
        <input
          aria-label={`År med ${skill.name}`}
          type="number"
          min={0}
          max={50}
          value={years}
          onChange={(e) => setYears(Number(e.target.value))}
        />
      </td>
      <td className="actions">
        <button type="button" disabled={!changed} onClick={save}>
          Spara
        </button>
        <button type="button" className="danger" onClick={remove}>
          Ta bort
        </button>
      </td>
    </tr>
  )
}

/** Add, change and remove skills. Names and synonyms resolve to the vocabulary name, as when Claude adds them. */
export function SkillsEditor({ skills, onChanged }: { skills: ProfileSkill[]; onChanged: () => void }) {
  const [name, setName] = useState('')
  const [level, setLevel] = useState<SkillLevel>('Intermediate')
  const [years, setYears] = useState(1)
  const [suggestions, setSuggestions] = useState<SkillSuggestion[]>([])
  const [notice, setNotice] = useState<string>()
  const [error, setError] = useState<string>()

  // Suggestions while typing, after a short pause.
  useEffect(() => {
    if (name.trim().length < 2) return
    const controller = new AbortController()
    const timer = setTimeout(() => {
      searchSkills(name, controller.signal).then(setSuggestions, () => setSuggestions([]))
    }, 250)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [name])

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    setError(undefined)
    setNotice(undefined)
    addSkill({ name, level, yearsOfExperience: years }).then(
      (added) => {
        if (!added.inVocabulary) {
          setNotice(`${added.name} finns inte i ordlistan som annonserna analyseras med, så den kommer aldrig att matcha en annons.`)
        }
        setName('')
        setSuggestions([])
        onChanged()
      },
      (err: unknown) => setError(message(err)),
    )
  }

  const visibleSuggestions = name.trim().length < 2 ? [] : suggestions.filter((s) => s.name !== name)

  return (
    <>
      {skills.length === 0 ? (
        <p className="muted">Inga kompetenser än. Lägg till din första nedan.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Kompetens</th>
              <th scope="col">Nivå</th>
              <th scope="col">År</th>
              <th scope="col">
                <span className="visually-hidden">Åtgärder</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {skills.map((skill) => (
              <SkillRow key={`${skill.name}-${skill.level}-${skill.yearsOfExperience}`} skill={skill} onChanged={onChanged} onError={setError} />
            ))}
          </tbody>
        </table>
      )}

      <form className="add-skill" onSubmit={onSubmit} aria-label="Lägg till kompetens">
        <input aria-label="Kompetens" placeholder="t.ex. Kafka eller c sharp" value={name} onChange={(e) => setName(e.target.value)} required />
        <LevelSelect label="Nivå" value={level} onChange={setLevel} />
        <input aria-label="År" type="number" min={0} max={50} value={years} onChange={(e) => setYears(Number(e.target.value))} />
        <button type="submit">Lägg till</button>
      </form>
      {visibleSuggestions.length > 0 && (
        <ul className="suggestions" aria-label="Förslag">
          {visibleSuggestions.map((s) => (
            <li key={s.name}>
              <button type="button" onClick={() => setName(s.name)}>
                {s.name}
              </button>
            </li>
          ))}
        </ul>
      )}
      {notice && <p className="notice">{notice}</p>}
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
    </>
  )
}
