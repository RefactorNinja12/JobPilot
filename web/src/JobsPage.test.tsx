import { fireEvent, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { JobsPage } from './JobsPage'
import { anAssessedJob, respondToInterestCalls, respondWithAssessedJobs } from './test/server'

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

  it('shows which skills the ad asks for that the profile has and which it lacks', async () => {
    respondWithAssessedJobs([anAssessedJob({ matchedSkills: ['C#', 'SQL'], missingSkills: ['Kafka'] })])

    render(<JobsPage />)

    const has = await screen.findByRole('list', { name: 'Du har' })
    expect(within(has).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['C#', 'SQL'])
    const lacks = screen.getByRole('list', { name: 'Du saknar' })
    expect(within(lacks).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['Kafka'])
  })

  it('shows no skill rows when the skills are not known', async () => {
    respondWithAssessedJobs([anAssessedJob({ matchedSkills: [], missingSkills: [] })])

    render(<JobsPage />)

    await screen.findByRole('link', { name: 'Backendutvecklare C#' })
    expect(screen.queryByRole('list', { name: 'Du har' })).not.toBeInTheDocument()
    expect(screen.queryByRole('list', { name: 'Du saknar' })).not.toBeInTheDocument()
  })

  it('shows the ads worth applying for by default, and the ones Claude sifted out on the other tab', async () => {
    respondWithAssessedJobs([
      anAssessedJob({ adId: 'good', headline: 'Bra', chance: 85 }),
      anAssessedJob({ adId: 'edge', headline: 'Precis', chance: 50 }),
      anAssessedJob({ adId: 'poor', headline: 'Dålig', chance: 10, municipality: null }),
    ])
    render(<JobsPage />)

    const goodTab = await screen.findByRole('tab', { name: 'Värda att söka (2)' })
    expect(goodTab).toHaveAttribute('aria-selected', 'true')
    expect(screen.getAllByRole('link').map((l) => l.textContent)).toEqual(['Bra', 'Precis'])

    fireEvent.click(screen.getByRole('tab', { name: 'Bortsållade (1)' }))

    expect(screen.getByRole('tab', { name: 'Bortsållade (1)' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getAllByRole('link').map((l) => l.textContent)).toEqual(['Dålig'])
    expect(screen.getByText('10 %')).toBeInTheDocument()
    expect(screen.getByText(/Ort saknas/)).toBeInTheDocument()
  })

  it('says so when a tab is empty', async () => {
    respondWithAssessedJobs([anAssessedJob({ chance: 20 })])
    render(<JobsPage />)

    expect(await screen.findByText('Inga annonser med god chans just nu.')).toBeInTheDocument()
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

  describe('wanting to apply', () => {
    const tabNamed = (name: RegExp) => screen.getByRole('tab', { name })

    it('marks an ad with the button, counts it in the "Vill söka" tab and sends the choice', async () => {
      const calls = respondToInterestCalls()
      respondWithAssessedJobs([anAssessedJob({ adId: 'a1' })])
      render(<JobsPage />)

      await userEvent.click(await screen.findByRole('button', { name: 'Jag vill söka' }))

      expect(await screen.findByRole('button', { name: /Du vill söka/ })).toHaveAttribute('aria-pressed', 'true')
      expect(tabNamed(/Vill söka \(1\)/)).toBeInTheDocument()
      expect(calls).toEqual([{ adId: 'a1', interested: true }])
    })

    it('shows an ad that was marked before, and takes the mark away again', async () => {
      const calls = respondToInterestCalls()
      respondWithAssessedJobs([anAssessedJob({ adId: 'a1', interested: true })])
      render(<JobsPage />)

      await userEvent.click(await screen.findByRole('button', { name: /Du vill söka/ }))

      expect(await screen.findByRole('button', { name: 'Jag vill söka' })).toHaveAttribute('aria-pressed', 'false')
      expect(tabNamed(/Vill söka \(0\)/)).toBeInTheDocument()
      expect(calls).toEqual([{ adId: 'a1', interested: false }])
    })

    it('lists only the marked ads in the "Vill söka" tab, whatever their chance', async () => {
      respondWithAssessedJobs([
        anAssessedJob({ adId: 'good', headline: 'Bra', chance: 85 }),
        anAssessedJob({ adId: 'poor', headline: 'Dålig men vill ändå', chance: 10, interested: true }),
      ])
      render(<JobsPage />)
      await screen.findByRole('link', { name: 'Bra' })

      await userEvent.click(tabNamed(/Vill söka/))

      expect(screen.getByRole('link', { name: 'Dålig men vill ändå' })).toBeInTheDocument()
      expect(screen.queryByRole('link', { name: 'Bra' })).not.toBeInTheDocument()
    })

    it('says so when nothing is marked yet', async () => {
      respondWithAssessedJobs([anAssessedJob()])
      render(<JobsPage />)
      await screen.findByRole('link', { name: 'Backendutvecklare C#' })

      await userEvent.click(tabNamed(/Vill söka/))

      expect(screen.getByText(/Du har inte markerat någon annons än/)).toBeInTheDocument()
    })

    it('puts the mark back and shows why when the API says no', async () => {
      respondToInterestCalls({ status: 404, body: { code: 'AdInterest.AdNotAssessed', detail: "The ad 'a1' has not been assessed." } })
      respondWithAssessedJobs([anAssessedJob({ adId: 'a1' })])
      render(<JobsPage />)

      await userEvent.click(await screen.findByRole('button', { name: 'Jag vill söka' }))

      expect(await screen.findByRole('alert')).toHaveTextContent("The ad 'a1' has not been assessed.")
      expect(screen.getByRole('button', { name: 'Jag vill söka' })).toHaveAttribute('aria-pressed', 'false')
      expect(tabNamed(/Vill söka \(0\)/)).toBeInTheDocument()
    })
  })
})
