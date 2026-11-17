/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    // Same origin as the page, so the API needs no CORS. Start the API with: dotnet run --project src/Jobpilot.Api
    proxy: { '/api': 'http://localhost:5299' },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
