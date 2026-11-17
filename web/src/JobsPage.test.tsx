import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { JobsPage } from './JobsPage'
import { aMatch, aResult, respondWithJobs } from './test/server'

describe('JobsPage', () => {
  it('shows each match with score, link, details and skills', async () => {
    respondWithJobs(aResult())

    render(<JobsPage />)

    const link = await screen.findByRole('link', { name: 'Backendutvecklare C#' })
    expect(link).toHaveAttribute('href', 'https://arbetsformedlingen.se/platsbanken/annonser/31744367')
    expect(link).toHaveAttribute('target', '_blank')
    expect(screen.getByText('85')).toBeInTheDocument()
    expect(screen.getByText(/Exempel AB · Göteborg · Heltid · Sista ansökan/)).toBeInTheDocument()
    const matched = screen.getByRole('list', { name: 'Matchade kompetenser' })
    expect(within(matched).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['C#', '.net'])
    const missing = screen.getByRole('list', { name: 'Saknade kompetenser' })
    expect(within(missing).getByText('Kafka')).toBeInTheDocument()
  })

  it('summarises what was fetched and why ads were left out', async () => {
    respondWithJobs(aResult({ fetched: 200, passed: 150, rejected: { TooFarAway: 42, StaffingAgency: 8 } }))

    render(<JobsPage />)

    expect(
      await screen.findByText('200 hämtade · 150 klarade dina krav · bortsorterade: 42 för långt bort, 8 bemanningsföretag'),
    ).toBeInTheDocument()
  })

  it("shows Claude's saved assessment and what could not be checked", async () => {
    respondWithJobs(
      aResult({
        matches: [
          aMatch({
            assessment: { chance: 70, summary: 'Stark C#-match, Kafka saknas.', assessedAt: '2026-11-17T18:00:00Z' },
            unknowns: ['Location'],
            municipality: null,
          }),
        ],
      }),
    )

    render(<JobsPage />)

    expect(await screen.findByText('Claudes bedömning: 70 % chans.')).toBeInTheDocument()
    expect(screen.getByText(/Stark C#-match, Kafka saknas\./)).toBeInTheDocument()
    expect(screen.getByText('Kunde inte kontrollera avståndet (annonsen saknar plats).')).toBeInTheDocument()
    expect(screen.getByText(/Ort saknas/)).toBeInTheDocument()
  })

  it('explains how to choose occupations when none are set', async () => {
    respondWithJobs({ code: 'FindJobs.NoOccupations', detail: 'No occupations.' }, 400)

    render(<JobsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Du har inga målyrken än')
  })

  it('says a service is down on 503', async () => {
    respondWithJobs({ code: 'JobSearch.Unavailable', detail: 'JobSearch could not be used: HTTP 503.' }, 503)

    render(<JobsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('En tjänst svarar inte just nu')
  })

  it('says so when nothing passed the dealbreakers', async () => {
    respondWithJobs(aResult({ matches: [], passed: 0 }))

    render(<JobsPage />)

    expect(await screen.findByText('Inga annonser klarade dina krav just nu.')).toBeInTheDocument()
  })
})
