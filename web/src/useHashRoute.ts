import { useEffect, useState } from 'react'

export type Route = 'jobs' | 'profile' | 'documents'

const routeFromHash = (hash: string): Route =>
  hash === '#/profil' ? 'profile' : hash === '#/dokument' ? 'documents' : 'jobs'

/** Tiny hash router: "#/profil" is the profile, "#/dokument" the CV and letter, anything else the job list. */
export function useHashRoute(): Route {
  const [route, setRoute] = useState(() => routeFromHash(window.location.hash))

  useEffect(() => {
    const onChange = () => setRoute(routeFromHash(window.location.hash))
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])

  return route
}
