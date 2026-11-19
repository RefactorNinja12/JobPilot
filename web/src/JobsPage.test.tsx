import { render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { JobsPage } from './JobsPage'
import { anAssessedJob, respondWithAssessedJobs } from './test/server'

describe('JobsPage', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 19, 12, 0)) // 19 Nov 2026, local time
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it("shows each assessed ad with Claude's chance, reasons, link and days left", async () => {
    respondWithAssessedJobs([anAssessedJob()])

    render(<JobsPage />)

    const link = await screen.findByRole('link', { name: 'Backendutvecklare C#' })
    expect(link).toHaveAttribute('href', 'https://arbetsformedlingen.se/platsbanken/annonser/31744367')
    expect(link).toHaveAttribute('target', '_blank')
    expect(screen.getByText('70 %')).toBeInTheDocument()
    expect(screen.getByText(/Stark C#-match, Kafka saknas\./)).toBeInTheDocument()
    expect(screen.getByText(/Exempel AB · Göteborg · Sista ansökan/)).toBeInTheDocument()
    expect(screen.getByText('12 dagar kvar att söka')).toBeInTheDocument()
  })

  it('shows poor chances too, in the order the API gives (best first)', async () => {
    respondWithAssessedJobs([
      anAssessedJob({ adId: 'good', headline: 'Bra', chance: 85 }),
      anAssessedJob({ adId: 'poor', headline: 'Dålig', chance: 10, municipality: null }),
    ])

    render(<JobsPage />)

    const links = await screen.findAllByRole('link', { name: /Bra|Dålig/ })
    expect(links.map((l) => l.textContent)).toEqual(['Bra', 'Dålig'])
    expect(screen.getByText('10 %')).toBeInTheDocument()
    expect(screen.getByText(/Ort saknas/)).toBeInTheDocument()
    expect(screen.getByText(/2 annonser bedömda av Claude/)).toBeInTheDocument()
  })

  it('tells how to get assessments when there are none', async () => {
    respondWithAssessedJobs([])

    render(<JobsPage />)

    expect(await screen.findByText(/Inga bedömda annonser än/)).toBeInTheDocument()
  })

  it('says a service is down on 503', async () => {
    respondWithAssessedJobs({ code: 'Database.Unavailable', detail: 'The database could not be reached.' }, 503)

    render(<JobsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('En tjänst svarar inte just nu')
  })
})
