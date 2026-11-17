import { JobsPage } from './JobsPage'
import './App.css'

export default function App() {
  return (
    <main>
      <header>
        <h1>Jobpilot</h1>
        <p className="tagline">Jobb som matchar din profil, bäst först.</p>
      </header>
      <JobsPage />
    </main>
  )
}
