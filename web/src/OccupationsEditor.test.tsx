import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Occupation } from './api/profile'
import { OccupationsEditor } from './OccupationsEditor'
import { respondToOccupationCalls } from './test/server'

const backend: Occupation = { taxonomyId: '7wdX_4rv_33z', label: 'Backend-utvecklare' }
const fullstack: Occupation = { taxonomyId: '71Ji_irM_rSJ', label: 'Fullstack-utvecklare' }

describe('OccupationsEditor', () => {
  it('suggests occupations while typing and adds the chosen one to the list', async () => {
    const calls = respondToOccupationCalls({ suggestions: [backend, fullstack] })
    const onChanged = vi.fn()
    render(<OccupationsEditor occupations={[backend]} onChanged={onChanged} />)

    await userEvent.type(screen.getByRole('textbox', { name: 'Sök yrke' }), 'utvecklare')
    await userEvent.click(await screen.findByRole('button', { name: '+ Fullstack-utvecklare' }))

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled())
    expect(calls).toEqual([{ method: 'PUT', body: [backend, fullstack] }])
    expect(screen.getByRole('textbox', { name: 'Sök yrke' })).toHaveValue('')
  })

  it('does not suggest occupations already chosen', async () => {
    respondToOccupationCalls({ suggestions: [backend, fullstack] })
    render(<OccupationsEditor occupations={[backend]} onChanged={vi.fn()} />)

    await userEvent.type(screen.getByRole('textbox', { name: 'Sök yrke' }), 'utvecklare')

    expect(await screen.findByRole('button', { name: '+ Fullstack-utvecklare' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '+ Backend-utvecklare' })).not.toBeInTheDocument()
  })

  it('removes an occupation by saving the list without it', async () => {
    const calls = respondToOccupationCalls({})
    const onChanged = vi.fn()
    render(<OccupationsEditor occupations={[backend, fullstack]} onChanged={onChanged} />)

    await userEvent.click(screen.getByRole('button', { name: 'Ta bort Backend-utvecklare' }))

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled())
    expect(calls).toEqual([{ method: 'PUT', body: [fullstack] }])
  })

  it('shows the API message when saving fails', async () => {
    respondToOccupationCalls({
      answer: { status: 400, body: { code: 'Profile.TooManyTargetOccupations', detail: 'A profile can have at most 10 target occupations.' } },
    })
    const onChanged = vi.fn()
    render(<OccupationsEditor occupations={[backend]} onChanged={onChanged} />)

    await userEvent.click(screen.getByRole('button', { name: 'Ta bort Backend-utvecklare' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('A profile can have at most 10 target occupations.')
    expect(onChanged).not.toHaveBeenCalled()
  })

  it('hides the search when ten occupations are chosen', () => {
    const ten = Array.from({ length: 10 }, (_, i) => ({ taxonomyId: `id${i}`, label: `Yrke ${i}` }))
    render(<OccupationsEditor occupations={ten} onChanged={vi.fn()} />)

    expect(screen.queryByRole('textbox', { name: 'Sök yrke' })).not.toBeInTheDocument()
    expect(screen.getByText(/vilket är max/)).toBeInTheDocument()
  })
})
