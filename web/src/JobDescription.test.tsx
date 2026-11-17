import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { JobDescription } from './JobDescription'
import { aMatch, respondWithJob } from './test/server'

const anAd = { ...aMatch(), description: 'Vi söker en backendutvecklare.\nDu kan C#.' }

describe('JobDescription', () => {
  it('fetches and shows the full ad when opened, keeping its line breaks', async () => {
    const requests = respondWithJob(anAd)
    render(<JobDescription adId="31744367" />)

    await userEvent.click(screen.getByRole('button', { name: 'Läs annonsen' }))

    const text = await screen.findByText(/Vi söker en backendutvecklare\./)
    expect(text.textContent).toBe('Vi söker en backendutvecklare.\nDu kan C#.')
    expect(screen.getByRole('button', { name: 'Dölj annonsen' })).toHaveAttribute('aria-expanded', 'true')
    expect(requests).toEqual(['31744367'])
  })

  it('hides it again and does not fetch twice', async () => {
    const requests = respondWithJob(anAd)
    render(<JobDescription adId="31744367" />)
    const button = screen.getByRole('button', { name: 'Läs annonsen' })

    await userEvent.click(button)
    await screen.findByText(/Vi söker/)
    await userEvent.click(button)
    expect(screen.queryByText(/Vi söker/)).not.toBeInTheDocument()
    await userEvent.click(button)

    expect(await screen.findByText(/Vi söker/)).toBeInTheDocument()
    expect(requests).toHaveLength(1)
  })

  it('says when the ad has been removed', async () => {
    respondWithJob({ code: 'JobSearch.AdNotFound', detail: 'No such ad.' }, 404)
    render(<JobDescription adId="0000000" />)

    await userEvent.click(screen.getByRole('button', { name: 'Läs annonsen' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Annonsen finns inte längre i Platsbanken.')
  })

  it('says when it could not be fetched', async () => {
    respondWithJob({ code: 'JobSearch.Unavailable', detail: 'Down.' }, 503)
    render(<JobDescription adId="1" />)

    await userEvent.click(screen.getByRole('button', { name: 'Läs annonsen' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Kunde inte hämta annonsen.')
  })
})
