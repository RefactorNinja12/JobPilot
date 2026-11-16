# Jobpilot

Personal job-search assistant built on the JobTech APIs (JobSearch, JobAd Enrichments, Taxonomy).
Single user. Runs on a local Kubernetes cluster (WSL) and is reached over Tailscale.

## Stack
- Backend: C# / .NET 10, Clean Architecture, PostgreSQL via EF Core
- Frontend: React + TypeScript (Vite) in `web/`
- AI: Claude through MCP (`Jobpilot.Mcp`). The backend never calls an LLM itself.
  Dealbreakers and fit-scoring are deterministic code; Claude handles chance score, explanations, CVs and cover letters.

## Architecture (dependencies point inwards only, enforced by `tests/Jobpilot.Architecture.Tests`)
- `Jobpilot.Domain`: entities, value objects, business rules. No dependencies.
- `Jobpilot.Application`: use cases, organized as feature folders (`Profiles/AddSkill/...`) plus abstractions (interfaces).
- `Jobpilot.Infrastructure`: EF Core and JobTech HTTP clients.
- `Jobpilot.Api` / `Jobpilot.Mcp` / `Jobpilot.Worker`: entry points that call the same Application handlers.

Conventions:
- Use `Result<T>` for expected failures. Exceptions are only for bugs.
- No MediatR. Inject plain handler classes.
- The profile is the source of truth. Generated CVs and letters must only use facts from the profile.

## Commits (important)
- One small, reviewable change per commit, aimed at under ~200–300 changed lines.
- Tests go in the same commit as the code they cover.
- Every commit must build and pass `dotnet test`.
- Conventional Commits: `feat(scope):`, `fix:`, `test:`, `refactor:`, `ci:`, `chore:`, `docs:`.
- Never commit directly to `main`.

### Workflow: one commit, one branch, one PR
1. `git switch main && git pull`: always start from an up-to-date `main`.
2. `git switch -c <type>/<short-name>`, e.g. `feat/skill-value-object`.
3. Make one small change, then commit (build and tests green).
4. `git push -u origin <branch>` and open a PR against `main`.
5. Wait until the PR is merged before starting the next branch from the updated `main`.
6. After merge, delete the branch locally and remotely.

## Testing
| Level | Project | Tools |
|---|---|---|
| Unit | `*.UnitTests` | xUnit v3, Shouldly, NSubstitute |
| Architecture | `Jobpilot.Architecture.Tests` | NetArchTest |
| Integration | `*.IntegrationTests` | Testcontainers (Postgres), WireMock.Net, WebApplicationFactory |
| System/E2E | `Jobpilot.SystemTests` | Playwright |
| Frontend | `web/` | Vitest, React Testing Library, MSW |

Automated tests never hit the real JobTech API; it is mocked with WireMock.

## MCP servers (`.mcp.json`)
- `microsoft-learn`: current .NET/EF Core/ASP.NET docs. Prefer it over memory for API details.
- `github`: PRs, CI results, review comments. Needs `GITHUB_PAT` exported in the shell (never commit tokens).

## Commands
```bash
dotnet build Jobpilot.slnx
dotnet test --solution Jobpilot.slnx
dotnet format Jobpilot.slnx --verify-no-changes   # CI fails if this fails
```
Test runner: Microsoft.Testing.Platform (configured in `global.json`).
