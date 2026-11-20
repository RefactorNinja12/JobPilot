import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import App from './App'
import { anAssessedJob, aProfile, respondToDocumentCalls, respondWithAssessedJobs, respondWithProfile } from './test/server'

afterEach(() => {
  window.location.hash = ''
})

describe('App', () => {
  it('starts on the job list', async () => {
    respondWithAssessedJobs([anAssessedJob()])

    render(<App />)

    expect(screen.getByRole('heading', { level: 1, name: 'Jobpilot' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Jobb' })).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByRole('link', { name: 'Backendutvecklare C#' })).toBeInTheDocument()
  })

  it('switches to the profile on #/profil', async () => {
    respondWithAssessedJobs([anAssessedJob()])
    respondWithProfile(aProfile())
    render(<App />)

    act(() => {
      window.location.hash = '#/profil'
      window.dispatchEvent(new HashChangeEvent('hashchange'))
    })

    expect(await screen.findByRole('heading', { name: 'Kompetenser' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Profil' })).toHaveAttribute('aria-current', 'page')
  })

  it('switches to the CV and letter on #/dokument', async () => {
    respondWithAssessedJobs([anAssessedJob()])
    respondToDocumentCalls()
    render(<App />)

    act(() => {
      window.location.hash = '#/dokument'
      window.dispatchEvent(new HashChangeEvent('hashchange'))
    })

    expect(await screen.findByRole('textbox', { name: 'CV' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Dokument' })).toHaveAttribute('aria-current', 'page')
  })
})
