#!/usr/bin/env bash
# Shared by the scripts that run Claude headless against the Jobpilot MCP server (run-assessment.sh, run-support.sh).
# Source it, then call run_claude_task. The backend never calls an LLM itself; these scripts are the triggers.
#
# Settings (environment, or ~/.config/jobpilot/env which is sourced if present; never commit that file):
#   JOBPILOT_MCP_URL         MCP endpoint, default http://localhost:5199/mcp (needs a port-forward).
#                            For cron use the Tailscale URL, which needs no port-forward.
#   JOBPILOT_MODEL           default sonnet
#   CLAUDE_BIN               path to claude, default: claude on PATH, else ~/.local/bin/claude
#   CLAUDE_CODE_OAUTH_TOKEN  only if the normal login is not available to cron (see `claude setup-token`)
#
# Every run is logged as JSON (cost, tokens, result) in ~/.local/state/jobpilot/runs/.

ENV_FILE="${JOBPILOT_ENV_FILE:-$HOME/.config/jobpilot/env}"
if [[ -f "$ENV_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
fi

STATE_DIR="${XDG_STATE_HOME:-$HOME/.local/state}/jobpilot"

# run_claude_task NAME PROMPT_FILE TOOLS MAX_BUDGET_USD [--dry-run]
#   NAME         used for the lock file, the log file name and the messages, e.g. "assessment"
#   TOOLS        comma separated MCP tools Claude may call; nothing else can be called
#   MAX_BUDGET   safety cap for one run, in dollars (API prices; a subscription is not billed this way)
run_claude_task() {
  local name="$1" prompt_file="$2" tools="$3" max_budget="$4" dry_run=false
  [[ "${5:-}" == "--dry-run" ]] && dry_run=true

  local mcp_url="${JOBPILOT_MCP_URL:-http://localhost:5199/mcp}"
  local model="${JOBPILOT_MODEL:-sonnet}"
  local claude_bin="${CLAUDE_BIN:-$(command -v claude || echo "$HOME/.local/bin/claude")}"
  local runs_dir="$STATE_DIR/runs"

  for tool in jq curl flock; do
    command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; return 1; }
  done
  [[ -x "$claude_bin" ]] || { echo "Cannot find claude (looked for $claude_bin). Set CLAUDE_BIN." >&2; return 1; }
  mkdir -p "$runs_dir"

  # Any HTTP answer (even 405 for a plain GET) means the server is up; no answer means it is not.
  if ! curl -s -o /dev/null -m 10 "$mcp_url"; then
    echo "Cannot reach the Jobpilot MCP server at $mcp_url" >&2
    return 1
  fi

  # Only the tools the task needs: fewer tool definitions in the prompt, and nothing else can be called.
  local mcp_config
  mcp_config="$(mktemp)"
  # shellcheck disable=SC2064
  trap "rm -f '$mcp_config'" RETURN
  jq -n --arg url "$mcp_url" '{mcpServers: {jobpilot: {type: "http", url: $url}}}' >"$mcp_config"

  local command_line=("$claude_bin" -p --model "$model" --mcp-config "$mcp_config" --strict-mcp-config
    --tools "" --allowedTools "$tools" --max-budget-usd "$max_budget" --no-session-persistence --output-format json)

  if $dry_run; then
    echo "OK: MCP server reachable at $mcp_url"
    echo "Would run (prompt from $prompt_file on stdin):"
    printf '  %q' "${command_line[@]}"
    echo
    return 0
  fi

  # One run of this task at a time, so a slow run and the next scheduled one never overlap.
  exec 9>"$STATE_DIR/lock-$name"
  flock -n 9 || { echo "Another $name run is in progress; skipping." >&2; return 0; }

  local log
  log="$runs_dir/$( [[ "$name" == assessment ]] || echo "$name-" )$(date +%Y%m%d-%H%M%S).json"
  # Start outside the repo so Claude does not load the project's CLAUDE.md (it is not needed and costs tokens).
  cd "$STATE_DIR" || return 1
  echo "==> $(date '+%F %T') $name with $model (budget cap \$$max_budget)"
  "${command_line[@]}" <"$prompt_file" >"$log" || {
    echo "Claude failed; see $log" >&2
    return 1
  }

  jq -r '"Result: \(.result // "(none)")"' "$log"
  jq -r '"Cost: $\(.total_cost_usd // 0 | tostring)  turns: \(.num_turns // 0)  tokens in/out: \(.usage.input_tokens // 0)/\(.usage.output_tokens // 0)  cache read/write: \(.usage.cache_read_input_tokens // 0)/\(.usage.cache_creation_input_tokens // 0)"' "$log"
  echo "Log: $log"
  [[ "$(jq -r '.is_error // false' "$log")" == "false" ]] || return 1
}
