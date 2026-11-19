#!/usr/bin/env bash
# Runs Claude headless against the Jobpilot MCP server: it fetches new matching ads (find_jobs) and saves an
# assessment of each (save_assessment), so the web UI has them ready. The backend never calls an LLM itself;
# this script is the trigger. Run it by hand to test, or from cron.
#
#   scripts/run-assessment.sh             run once
#   scripts/run-assessment.sh --dry-run   check the setup and print the command, without calling Claude
#
# Settings (environment, or ~/.config/jobpilot/env which is sourced if present; never commit that file):
#   JOBPILOT_MCP_URL         MCP endpoint, default http://localhost:5199/mcp (needs a port-forward).
#                            For cron use the Tailscale URL, which needs no port-forward.
#   JOBPILOT_MODEL           default sonnet
#   JOBPILOT_MAX_BUDGET_USD  safety cap for one run, default 1.50
#   CLAUDE_BIN               path to claude, default: claude on PATH, else ~/.local/bin/claude
#   CLAUDE_CODE_OAUTH_TOKEN  only if the normal login is not available to cron (see `claude setup-token`)
#
# Every run is logged as JSON (cost, tokens, result) in ~/.local/state/jobpilot/runs/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${JOBPILOT_ENV_FILE:-$HOME/.config/jobpilot/env}"
if [[ -f "$ENV_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
fi

DRY_RUN=false
[[ "${1:-}" == "--dry-run" ]] && DRY_RUN=true

MCP_URL="${JOBPILOT_MCP_URL:-http://localhost:5199/mcp}"
MODEL="${JOBPILOT_MODEL:-sonnet}"
MAX_BUDGET="${JOBPILOT_MAX_BUDGET_USD:-1.50}"
CLAUDE_BIN="${CLAUDE_BIN:-$(command -v claude || echo "$HOME/.local/bin/claude")}"
STATE_DIR="${XDG_STATE_HOME:-$HOME/.local/state}/jobpilot"
RUNS_DIR="$STATE_DIR/runs"
PROMPT_FILE="$ROOT/scripts/assess-prompt.md"

for tool in jq curl flock; do
  command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; exit 1; }
done
[[ -x "$CLAUDE_BIN" ]] || { echo "Cannot find claude (looked for $CLAUDE_BIN). Set CLAUDE_BIN." >&2; exit 1; }
mkdir -p "$RUNS_DIR"

# Any HTTP answer (even 405 for a plain GET) means the server is up; no answer means it is not.
if ! curl -s -o /dev/null -m 10 "$MCP_URL"; then
  echo "Cannot reach the Jobpilot MCP server at $MCP_URL" >&2
  exit 1
fi

# Only the three tools the task needs: fewer tool definitions in the prompt, and nothing else can be called.
TOOLS="mcp__jobpilot__get_profile,mcp__jobpilot__find_jobs,mcp__jobpilot__save_assessment"
MCP_CONFIG="$(mktemp)"
trap 'rm -f "$MCP_CONFIG"' EXIT
jq -n --arg url "$MCP_URL" '{mcpServers: {jobpilot: {type: "http", url: $url}}}' >"$MCP_CONFIG"

command_line=("$CLAUDE_BIN" -p --model "$MODEL" --mcp-config "$MCP_CONFIG" --strict-mcp-config
  --tools "" --allowedTools "$TOOLS" --max-budget-usd "$MAX_BUDGET" --no-session-persistence --output-format json)

if $DRY_RUN; then
  echo "OK: MCP server reachable at $MCP_URL"
  echo "Would run (prompt from $PROMPT_FILE on stdin):"
  printf '  %q' "${command_line[@]}"
  echo
  exit 0
fi

# One run at a time, so a slow run and the next scheduled one never overlap.
exec 9>"$STATE_DIR/lock"
flock -n 9 || { echo "Another run is in progress; skipping." >&2; exit 0; }

LOG="$RUNS_DIR/$(date +%Y%m%d-%H%M%S).json"
# Start outside the repo so Claude does not load the project's CLAUDE.md (it is not needed and costs tokens).
cd "$STATE_DIR"
echo "==> $(date '+%F %T') assessing with $MODEL (budget cap \$$MAX_BUDGET)"
"${command_line[@]}" <"$PROMPT_FILE" >"$LOG" || {
  echo "Claude failed; see $LOG" >&2
  exit 1
}

jq -r '"Result: \(.result // "(none)")"' "$LOG"
jq -r '"Cost: $\(.total_cost_usd // 0 | tostring)  turns: \(.num_turns // 0)  tokens in/out: \(.usage.input_tokens // 0)/\(.usage.output_tokens // 0)  cache read/write: \(.usage.cache_read_input_tokens // 0)/\(.usage.cache_creation_input_tokens // 0)"' "$LOG"
echo "Log: $LOG"
[[ "$(jq -r '.is_error // false' "$LOG")" == "false" ]] || exit 1
