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

  it('shows target occupations and editable dealbreakers', async () => {
    respondWithProfile(aProfile())

    render(<ProfilePage />)

    expect(await screen.findByText('Backend-utvecklare')).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Heltid' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Deltid' })).not.toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Dölj annonser från bemanningsföretag' })).toBeChecked()
    expect(screen.getByRole('spinbutton', { name: 'Högst (km fågelvägen)' })).toHaveValue(40)
    expect(screen.getByRole('spinbutton', { name: 'Latitud' })).toHaveValue(57.7089)
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
    expect(screen.getByRole('textbox', { name: 'Sök yrke' })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Deltid' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Begränsa avståndet från hemmet' })).not.toBeChecked()
    expect(screen.queryByRole('spinbutton', { name: 'Latitud' })).not.toBeInTheDocument()
  })

  it('says when the profile could not be loaded', async () => {
    respondWithProfile({}, 500)

    render(<ProfilePage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Kunde inte hämta profilen')
  })
})
