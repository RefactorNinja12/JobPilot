import { describe, expect, it } from 'vitest'
import { apiUrl } from './http'

describe('apiUrl', () => {
  it('allows this site\'s /api/ paths, query included', () => {
    const url = apiUrl('/api/profile/skills?name=C%23')

    expect(url.origin).toBe(window.location.origin)
    expect(url.pathname).toBe('/api/profile/skills')
    expect(url.searchParams.get('name')).toBe('C#')
  })

  it.each([
    ['another server', 'https://evil.example/api/jobs'],
    ['a protocol-relative URL', '//evil.example/api/jobs'],
    ['a path outside the API', '/admin'],
    ['a path that escapes /api/', '/api/../admin'],
  ])('refuses %s', (_, path) => {
    expect(() => apiUrl(path)).toThrow(/only this site's \/api\/ is allowed/)
  })
})
