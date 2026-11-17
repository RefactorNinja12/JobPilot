# JobPilot

Personal job-search assistant built on the JobTech APIs. Claude talks to it over MCP.

## Run locally

Needs Docker and the .NET 10 SDK.

```bash
docker compose up -d                          # PostgreSQL on 127.0.0.1:5433
dotnet run --project src/Jobpilot.Worker      # migrates the database, then downloads the skill vocabulary
dotnet run --project src/Jobpilot.Mcp         # MCP server on http://localhost:5199/mcp
```

Start the Worker first and leave it running: it creates the schema and keeps the skill vocabulary
(~58k skills, ~6 s to download) up to date. The MCP server can start once the Worker logs
`Skill vocabulary updated`.

Claude Code picks up the `jobpilot` server from `.mcp.json`. Then ask, for example,
"add Kafka to my profile, advanced, 3 years".

| Tool | What it does |
|---|---|
| `search_skills` | Finds skill names and synonyms in the vocabulary that job ads are analysed with |
| `add_skill` | Adds a skill to the profile under its exact vocabulary name |

The local database has no password and is only reachable from this machine. `docker compose down -v`
deletes it.

## Docker images

```bash
docker build --target worker -t jobpilot-worker .
docker build --target mcp -t jobpilot-mcp .
```

Both run as a non-root user on chiseled Ubuntu. They need `ConnectionStrings__Jobpilot`; the MCP server listens
on port 8080 (`/mcp`, and `/health` for probes).

## Run in Kubernetes (k3d)

```bash
./scripts/deploy-local.sh
```

Creates (or reuses) a k3d cluster named `jobpilot`, builds both images, imports them and applies `deploy/k8s`:
PostgreSQL as a StatefulSet on a 5 Gi volume, the Worker (migrates, then refreshes the vocabulary daily) and the MCP
server. The database password is generated once into the `jobpilot-db` Secret and never written to the repo.
Run the script again after code changes to roll out new images.

```bash
kubectl --context k3d-jobpilot -n jobpilot port-forward svc/mcp 5199:80   # MCP at http://localhost:5199/mcp
```

`k3d cluster stop jobpilot` keeps the data; `k3d cluster delete jobpilot` removes it.

