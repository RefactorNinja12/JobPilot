import { useEffect, useState } from 'react'

export type Route = 'jobs' | 'profile'

const routeFromHash = (hash: string): Route => (hash === '#/profil' ? 'profile' : 'jobs')

/** Tiny hash router: "#/profil" is the profile, anything else the job list. No library needed for two pages. */
export function useHashRoute(): Route {
  const [route, setRoute] = useState(() => routeFromHash(window.location.hash))

  useEffect(() => {
    const onChange = () => setRoute(routeFromHash(window.location.hash))
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])

  return route
}
