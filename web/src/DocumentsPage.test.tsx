import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { BaseDocument } from './api/documents'
import { DocumentsPage } from './DocumentsPage'
import { respondToDocumentCalls } from './test/server'

const savedCv: BaseDocument = { kind: 'Cv', text: 'Anna Åberg\nSystemutvecklare', updatedAt: '2026-11-19T10:00:00Z' }
const docx = () =>
  new File(['innehåll'], 'cv.docx', { type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' })

const cv = () => screen.findByRole('textbox', { name: 'CV' })
const save = (name: string) =>
  // Each editor is its own form, named after its title.
  screen.getByRole('form', { name }).querySelector('button[type="submit"]') as HTMLButtonElement

describe('DocumentsPage', () => {
  it('shows the saved documents, and an empty box for one that has not been saved', async () => {
    respondToDocumentCalls({ documents: [savedCv] })

    render(<DocumentsPage />)

    expect(await cv()).toHaveValue('Anna Åberg\nSystemutvecklare')
    expect(screen.getByRole('textbox', { name: 'Personligt brev' })).toHaveValue('')
    expect(screen.getByText(/Sparad/)).toBeInTheDocument()
  })

  it('saves pasted text, and only when there is something new to save', async () => {
    const { saved } = respondToDocumentCalls()
    render(<DocumentsPage />)
    const letter = await screen.findByRole('textbox', { name: 'Personligt brev' })
    expect(save('Personligt brev')).toBeDisabled()

    await userEvent.type(letter, 'Hej!')
    await userEvent.click(save('Personligt brev'))

    await vi.waitFor(() => expect(saved).toEqual([{ kind: 'CoverLetter', text: 'Hej!' }]))
    await vi.waitFor(() => expect(save('Personligt brev')).toBeDisabled()) // Saved: nothing new to save.
    expect(screen.getByText(/Sparad/)).toBeInTheDocument()
  })

  it('puts the text of an uploaded Word file in the box without saving it', async () => {
    const { saved, uploads } = respondToDocumentCalls()
    render(<DocumentsPage />)
    await cv()

    await userEvent.upload(screen.getByLabelText('Word-fil för CV'), docx())

    expect(await screen.findByDisplayValue('Text ur Word-filen')).toBeInTheDocument()
    expect(uploads).toEqual(['innehåll'])
    expect(saved).toEqual([])
    expect(screen.getByRole('status')).toHaveTextContent(/Läs igenom den/)

    await userEvent.click(save('CV'))
    await vi.waitFor(() => expect(saved).toEqual([{ kind: 'Cv', text: 'Text ur Word-filen' }]))
  })

  it('shows the API message when the file is not a Word document, and keeps the text', async () => {
    respondToDocumentCalls({
      documents: [savedCv],
      extractAnswer: { status: 400, body: { code: 'DocxText.NotADocxFile', detail: 'The file is not a Word (.docx) document.' } },
    })
    render(<DocumentsPage />)
    await cv()

    await userEvent.upload(screen.getByLabelText('Word-fil för CV'), docx())

    expect(await screen.findByRole('alert')).toHaveTextContent('The file is not a Word (.docx) document.')
    expect(screen.getByRole('textbox', { name: 'CV' })).toHaveValue('Anna Åberg\nSystemutvecklare')
  })

  it('shows the API message when saving fails', async () => {
    respondToDocumentCalls({
      saveAnswer: { status: 400, body: { code: 'BaseDocument.TextTooLong', detail: 'The document must be at most 30000 characters.' } },
    })
    render(<DocumentsPage />)

    await userEvent.type(await cv(), 'Text')
    await userEvent.click(save('CV'))

    expect(await screen.findByRole('alert')).toHaveTextContent('The document must be at most 30000 characters.')
  })
})
