#!/usr/bin/env bash
# Runs Claude headless against the Jobpilot MCP server to prepare application support (keywords, what to lift, CV
# suggestions, a short letter draft) for the ads the user has marked "Jag vill söka" and that have none yet.
# Normally started by scripts/watch-wanted-ads.sh; run it by hand to test.
#
#   scripts/run-support.sh             run once
#   scripts/run-support.sh --dry-run   check the setup and print the command, without calling Claude
#
# Settings: see scripts/lib/claude-task.sh. Also JOBPILOT_SUPPORT_MAX_BUDGET_USD, the safety cap for one run (default 2.00).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lib/claude-task.sh
source "$ROOT/scripts/lib/claude-task.sh"

run_claude_task support "$ROOT/scripts/support-prompt.md" \
  "mcp__jobpilot__list_wanted_ads,mcp__jobpilot__get_profile,mcp__jobpilot__get_documents,mcp__jobpilot__get_job_ad,mcp__jobpilot__save_application_support" \
  "${JOBPILOT_SUPPORT_MAX_BUDGET_USD:-2.00}" "${1:-}"
