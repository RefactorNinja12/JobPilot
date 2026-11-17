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
