import { DocumentsPage } from './DocumentsPage'
import { JobsPage } from './JobsPage'
import { ProfilePage } from './ProfilePage'
import { useHashRoute } from './useHashRoute'
import './App.css'

export default function App() {
  const route = useHashRoute()

  return (
    <main>
      <header>
        <h1>Jobpilot</h1>
        <nav aria-label="Huvudmeny">
          <a href="#/" aria-current={route === 'jobs' ? 'page' : undefined}>
            Jobb
          </a>
          <a href="#/profil" aria-current={route === 'profile' ? 'page' : undefined}>
            Profil
          </a>
          <a href="#/dokument" aria-current={route === 'documents' ? 'page' : undefined}>
            Dokument
          </a>
        </nav>
      </header>
      {route === 'jobs' ? <JobsPage /> : route === 'profile' ? <ProfilePage /> : <DocumentsPage />}
    </main>
  )
}
