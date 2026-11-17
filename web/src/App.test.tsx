import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'
import { aResult, respondWithJobs } from './test/server'

describe('App', () => {
  it('shows the title and the job list', async () => {
    respondWithJobs(aResult())

    render(<App />)

    expect(screen.getByRole('heading', { level: 1, name: 'Jobpilot' })).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: 'Backendutvecklare C#' })).toBeInTheDocument()
  })
})
