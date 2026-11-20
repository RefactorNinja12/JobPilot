import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApplicationSupportPanel } from './ApplicationSupportPanel'
import { respondWithSupport, someSupport } from './test/server'

const clipboard = (writeText: (text: string) => Promise<void>) =>
  Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })

afterEach(() => vi.restoreAllMocks())

const open = async () => {
  render(<ApplicationSupportPanel adId="31744367" />)
  await userEvent.click(screen.getByRole('button', { name: 'Visa underlag för ansökan' }))
}

describe('ApplicationSupportPanel', () => {
  it('fetches the support when opened and shows every part', async () => {
    respondWithSupport(someSupport({ companyNotes: 'Bygger programvara för fordon.' }))

    await open()

    const keywords = await screen.findByRole('list', { name: 'Nyckelord' })
    expect(within(keywords).getAllByRole('listitem').map((li) => li.textContent)).toEqual(['C#', 'Kubernetes'])
    expect(screen.getByText(/Lyft C# och Postgres/)).toBeInTheDocument()
    expect(screen.getByText('Flytta upp projektet med Docker.')).toBeInTheDocument()
    expect(screen.getByText(/Jag söker tjänsten som backendutvecklare/)).toBeInTheDocument()
    expect(screen.getByText('Bygger programvara för fordon.')).toBeInTheDocument()
  })

  it('leaves out the parts Claude did not prepare', async () => {
    respondWithSupport(someSupport({ cvSuggestions: null, letterDraft: null, companyNotes: null }))

    await open()

    await screen.findByRole('list', { name: 'Nyckelord' })
    expect(screen.queryByRole('region', { name: 'Utkast till brev' })).not.toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Förslag på CV-ändringar' })).not.toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Om företaget' })).not.toBeInTheDocument()
  })

  it('copies the keywords together and a text on its own', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined)
    clipboard(writeText)
    respondWithSupport(someSupport())
    await open()

    await userEvent.click(await screen.findByRole('button', { name: 'Kopiera nyckelord' }))
    await userEvent.click(screen.getByRole('button', { name: 'Kopiera utkast till brev' }))

    expect(writeText).toHaveBeenNthCalledWith(1, 'C#, Kubernetes')
    expect(writeText).toHaveBeenNthCalledWith(2, 'Hej! Jag söker tjänsten som backendutvecklare.')
    expect(await screen.findAllByText('Kopierat ✓')).toHaveLength(2)
  })

  it('says so when the clipboard cannot be used', async () => {
    clipboard(vi.fn().mockRejectedValue(new Error('denied')))
    respondWithSupport(someSupport())
    await open()

    await userEvent.click(await screen.findByRole('button', { name: 'Kopiera nyckelord' }))

    expect(await screen.findByText('Kunde inte kopiera')).toBeInTheDocument()
  })

  it('hides the support again and does not fetch it twice', async () => {
    respondWithSupport(someSupport())
    await open()
    await screen.findByRole('list', { name: 'Nyckelord' })

    await userEvent.click(screen.getByRole('button', { name: 'Dölj underlag för ansökan' }))
    expect(screen.queryByRole('list', { name: 'Nyckelord' })).not.toBeInTheDocument()

    respondWithSupport({ code: 'ApplicationSupport.NotFound', detail: 'Should not be asked again.' }, 404)
    await userEvent.click(screen.getByRole('button', { name: 'Visa underlag för ansökan' }))
    expect(screen.getByRole('list', { name: 'Nyckelord' })).toBeInTheDocument()
  })

  it('says so when nothing was found', async () => {
    respondWithSupport({ code: 'ApplicationSupport.NotFound', detail: 'No support.' }, 404)

    await open()

    expect(await screen.findByRole('alert')).toHaveTextContent('Inget underlag hittades för annonsen.')
  })

  it('says so when the support cannot be fetched', async () => {
    respondWithSupport({ code: 'Something.Else', detail: 'Boom.' }, 500)

    await open()

    expect(await screen.findByRole('alert')).toHaveTextContent('Kunde inte hämta underlaget.')
  })
})
