import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ProfilePage } from './ProfilePage'
import { aProfile, respondWithProfile } from './test/server'

describe('ProfilePage', () => {
  it('lists skills with editable level and years', async () => {
    respondWithProfile(aProfile())

    render(<ProfilePage />)

    expect(await screen.findByRole('combobox', { name: 'Nivå för C#' })).toHaveValue('Expert')
    expect(screen.getByRole('spinbutton', { name: 'År med C#' })).toHaveValue(8)
    expect(screen.getByRole('combobox', { name: 'Nivå för Kafka' })).toHaveValue('Beginner')
  })

  it('shows target occupations and dealbreakers in plain Swedish', async () => {
    respondWithProfile(aProfile())

    render(<ProfilePage />)

    expect(await screen.findByText('Backend-utvecklare')).toBeInTheDocument()
    expect(screen.getByText('Bara heltid')).toBeInTheDocument()
    expect(screen.getByText('Visas inte')).toBeInTheDocument()
    expect(screen.getByText('Högst 40 km fågelvägen från hemmet (57.71, 11.97)')).toBeInTheDocument()
  })

  it('explains what to do when the profile is empty', async () => {
    respondWithProfile(
      aProfile({
        skills: [],
        targetOccupations: [],
        preferences: { acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false, commute: null },
      }),
    )

    render(<ProfilePage />)

    expect(await screen.findByText(/Inga kompetenser än/)).toBeInTheDocument()
    expect(screen.getByText(/Inga yrken valda/)).toBeInTheDocument()
    expect(screen.getByText('Heltid och deltid')).toBeInTheDocument()
    expect(screen.getByText('Visas')).toBeInTheDocument()
    expect(screen.getByText('Ingen gräns')).toBeInTheDocument()
  })

  it('says when the profile could not be loaded', async () => {
    respondWithProfile({}, 500)

    render(<ProfilePage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Kunde inte hämta profilen')
  })

  it('describes part-time only', async () => {
    respondWithProfile(aProfile({ preferences: { ...aProfile().preferences, acceptsFullTime: false, acceptsPartTime: true } }))

    render(<ProfilePage />)

    expect(await screen.findByText('Bara deltid')).toBeInTheDocument()
  })
})
