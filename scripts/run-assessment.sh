#!/usr/bin/env bash
# Runs Claude headless against the Jobpilot MCP server: it fetches new matching ads (find_jobs) and saves an
# assessment of each (save_assessment), so the web UI has them ready. Run it by hand to test, or from cron.
#
#   scripts/run-assessment.sh             run once
#   scripts/run-assessment.sh --dry-run   check the setup and print the command, without calling Claude
#
# Settings: see scripts/lib/claude-task.sh. Also JOBPILOT_MAX_BUDGET_USD, the safety cap for one run (default 1.50).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lib/claude-task.sh
source "$ROOT/scripts/lib/claude-task.sh"

run_claude_task assessment "$ROOT/scripts/assess-prompt.md" \
  "mcp__jobpilot__get_profile,mcp__jobpilot__find_jobs,mcp__jobpilot__save_assessment" \
  "${JOBPILOT_MAX_BUDGET_USD:-1.50}" "${1:-}"
