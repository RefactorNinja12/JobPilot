import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { deadlineText } from './deadline'

describe('deadlineText', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 19, 23, 30)) // late on 19 Nov 2026, local time
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it.each([
    ['2026-12-01', '12 dagar kvar att söka'],
    ['2026-11-20', '1 dag kvar att söka'],
    ['2026-11-19', 'Sista ansökningsdag idag'],
    ['2026-11-18', 'Sista ansökningsdag har passerat'],
  ])('deadline %s reads "%s"', (deadline, text) => {
    expect(deadlineText(deadline)).toBe(text)
  })
})
