# Jobpilot web UI

React + TypeScript (Vite). See "Web UI" in the root README for how to run it.

- `src/api/` mirrors the JSON from `Jobpilot.Api`; the UI never calls an AI.
- Tests use Vitest, Testing Library and MSW; any request without a handler fails the test.
