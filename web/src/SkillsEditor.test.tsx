import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ProfileSkill } from './api/profile'
import { SkillsEditor } from './SkillsEditor'
import { respondToSkillCalls } from './test/server'

const csharp: ProfileSkill = { name: 'C#', level: 'Expert', yearsOfExperience: 8 }

afterEach(() => vi.restoreAllMocks())

describe('SkillsEditor', () => {
  it('suggests vocabulary names while typing and adds the chosen one', async () => {
    const calls = respondToSkillCalls({ suggestions: [{ name: 'Kafka', terms: ['kafka', 'apache kafka'] }] })
    const onChanged = vi.fn()
    render(<SkillsEditor skills={[]} onChanged={onChanged} />)

    await userEvent.type(screen.getByRole('textbox', { name: 'Kompetens' }), 'apache')
    await userEvent.click(await screen.findByRole('button', { name: 'Kafka' }))
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Nivå' }), 'Avancerad')
    await userEvent.clear(screen.getByRole('spinbutton', { name: 'År' }))
    await userEvent.type(screen.getByRole('spinbutton', { name: 'År' }), '3')
    await userEvent.click(screen.getByRole('button', { name: 'Lägg till' }))

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled())
    expect(calls).toEqual([
      { method: 'POST', url: expect.stringContaining('/api/profile/skills'), body: { name: 'Kafka', level: 'Advanced', yearsOfExperience: 3 } },
    ])
    expect(screen.getByRole('textbox', { name: 'Kompetens' })).toHaveValue('')
  })

  it('warns when the skill is not in the vocabulary', async () => {
    respondToSkillCalls({ answer: { status: 200, body: { name: 'Elixirr', inVocabulary: false } } })
    render(<SkillsEditor skills={[]} onChanged={vi.fn()} />)

    await userEvent.type(screen.getByRole('textbox', { name: 'Kompetens' }), 'Elixirr')
    await userEvent.click(screen.getByRole('button', { name: 'Lägg till' }))

    expect(await screen.findByText(/Elixirr finns inte i ordlistan/)).toBeInTheDocument()
  })

  it('shows the API message when the skill is already there', async () => {
    respondToSkillCalls({
      answer: { status: 409, body: { code: 'Profile.SkillAlreadyAdded', detail: "The skill 'C#' is already in the profile." } },
    })
    render(<SkillsEditor skills={[csharp]} onChanged={vi.fn()} />)

    await userEvent.type(screen.getByRole('textbox', { name: 'Kompetens' }), 'c sharp')
    await userEvent.click(screen.getByRole('button', { name: 'Lägg till' }))

    expect(await screen.findByRole('alert')).toHaveTextContent("The skill 'C#' is already in the profile.")
  })

  it('saves a changed level and years, and only when something changed', async () => {
    const calls = respondToSkillCalls({ answer: { status: 200, body: csharp } })
    const onChanged = vi.fn()
    render(<SkillsEditor skills={[csharp]} onChanged={onChanged} />)
    const save = screen.getByRole('button', { name: 'Spara' })
    expect(save).toBeDisabled()

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Nivå för C#' }), 'Avancerad')
    await userEvent.click(save)

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled())
    expect(calls[0]).toEqual({
      method: 'PUT',
      url: expect.stringContaining('/api/profile/skills'),
      body: { name: 'C#', level: 'Advanced', yearsOfExperience: 8 },
    })
  })

  it('removes a skill after confirming, with the name encoded', async () => {
    const calls = respondToSkillCalls({ answer: { status: 200, body: 'C#' } })
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const onChanged = vi.fn()
    render(<SkillsEditor skills={[csharp]} onChanged={onChanged} />)

    await userEvent.click(screen.getByRole('button', { name: 'Ta bort' }))

    await vi.waitFor(() => expect(onChanged).toHaveBeenCalled())
    expect(calls[0].method).toBe('DELETE')
    expect(calls[0].url).toContain('name=C%23')
  })

  it('does nothing when removal is not confirmed', async () => {
    const calls = respondToSkillCalls({})
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<SkillsEditor skills={[csharp]} onChanged={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Ta bort' }))

    expect(calls).toEqual([])
  })
})
