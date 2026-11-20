#!/usr/bin/env bash
# Runs Claude headless against the Jobpilot MCP server: it fetches new matching ads (find_jobs) and saves an
# assessment of each (save_assessment), so the web UI has them ready. Run it by hand to test, or from cron.
#
#   scripts/run-assessment.sh             run once
#   scripts/run-assessment.sh --dry-run   check the setup and print the command, without calling Claude
#
# Once a day from cron (crontab -e), here at 07:30, using your own path:
#   30 7 * * * /home/rini/code/Jobpilot/scripts/run-assessment.sh >> ~/.local/state/jobpilot/assessment.log 2>&1
# Cron has no terminal to log in from: put JOBPILOT_MCP_URL (the Tailscale address) and CLAUDE_CODE_OAUTH_TOKEN (from
# `claude setup-token`) in ~/.config/jobpilot/env first. find_jobs is limited to 2 calls a day, so one scheduled run
# leaves one for manual use.
#
# Settings: see scripts/lib/claude-task.sh. Also JOBPILOT_MAX_BUDGET_USD, the safety cap for one run (default 1.50).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lib/claude-task.sh
source "$ROOT/scripts/lib/claude-task.sh"

run_claude_task assessment "$ROOT/scripts/assess-prompt.md" \
  "mcp__jobpilot__get_profile,mcp__jobpilot__find_jobs,mcp__jobpilot__save_assessment" \
  "${JOBPILOT_MAX_BUDGET_USD:-1.50}" "${1:-}"
