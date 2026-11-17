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
    coverage: {
      // Report every source file, so untested ones show as uncovered (SonarCloud reads coverage/lcov.info).
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/main.tsx', 'src/test/**', 'src/**/*.test.{ts,tsx}'],
      reporter: ['text-summary', 'lcov'],
    },
  },
})
