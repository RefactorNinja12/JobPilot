import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import App from './App'
import { aProfile, aResult, respondWithJobs, respondWithProfile } from './test/server'

afterEach(() => {
  window.location.hash = ''
})

describe('App', () => {
  it('starts on the job list', async () => {
    respondWithJobs(aResult())

    render(<App />)

    expect(screen.getByRole('heading', { level: 1, name: 'Jobpilot' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Jobb' })).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByRole('link', { name: 'Backendutvecklare C#' })).toBeInTheDocument()
  })

  it('switches to the profile on #/profil', async () => {
    respondWithJobs(aResult())
    respondWithProfile(aProfile())
    render(<App />)

    act(() => {
      window.location.hash = '#/profil'
      window.dispatchEvent(new HashChangeEvent('hashchange'))
    })

    expect(await screen.findByRole('heading', { name: 'Kompetenser' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Profil' })).toHaveAttribute('aria-current', 'page')
  })
})
