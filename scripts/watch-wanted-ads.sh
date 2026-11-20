#!/usr/bin/env bash
# Starts scripts/run-support.sh when the user has marked ads "Jag vill söka" that Claude has not prepared yet.
# Meant to run from cron every minute. The check is one cheap HTTP call to the web API and costs no tokens; Claude is
# only started when there is something to do. So pressing the button in the web UI is followed by Claude's work within
# about a minute (if this computer is on), and nothing is spent when there is nothing to prepare.
#
#   crontab -e, then add (use your own path):
#   * * * * * /home/rini/code/Jobpilot/scripts/watch-wanted-ads.sh >> ~/.local/state/jobpilot/watch.log 2>&1
#
# It only prints when it acts, so the log stays short. Guards against spending the quota in a loop:
#   - the same set of pending ads is not tried again until the cooldown has passed (a run that cannot finish an ad,
#     for example because get_job_ad has reached its daily limit, is not repeated every minute);
#   - at most JOBPILOT_WATCH_MAX_RUNS_PER_DAY runs per day;
#   - nothing is started while a support run is already going.
#
# Settings (environment, or ~/.config/jobpilot/env, see scripts/lib/claude-task.sh):
#   JOBPILOT_WEB_URL                       the web UI's address, default http://localhost:5299 (needs a port-forward);
#                                          for cron use the Tailscale address, which needs none
#   JOBPILOT_WATCH_COOLDOWN_MINUTES        default 60
#   JOBPILOT_WATCH_MAX_RUNS_PER_DAY        default 6
#   JOBPILOT_WATCH_VERBOSE=1               also say why nothing was started
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lib/claude-task.sh
source "$ROOT/scripts/lib/claude-task.sh"

WEB_URL="${JOBPILOT_WEB_URL:-http://localhost:5299}"
COOLDOWN_MINUTES="${JOBPILOT_WATCH_COOLDOWN_MINUTES:-60}"
MAX_RUNS_PER_DAY="${JOBPILOT_WATCH_MAX_RUNS_PER_DAY:-6}"
STATE_FILE="$STATE_DIR/watch-state.json"

say() { echo "$(date '+%F %T') $*"; }
why() { [[ "${JOBPILOT_WATCH_VERBOSE:-}" == "1" ]] && say "$*"; return 0; }

for tool in jq curl flock; do
  command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; exit 1; }
done
mkdir -p "$STATE_DIR"

# The web UI being down or the computer being offline is normal, not an error: stay quiet.
ads="$(curl -sf -m 15 "$WEB_URL/api/jobs/assessed")" || { why "web API not reachable at $WEB_URL"; exit 0; }

pending="$(jq -r '[.[] | select(.interested == true and .hasSupport == false) | .adId] | sort | join(",")' <<<"$ads")"
[[ -n "$pending" ]] || { why "nothing to prepare"; exit 0; }

# A support run is going: it will pick up what is pending, and may finish it before the next look.
if ! flock -n "$STATE_DIR/lock-support" true; then
  why "a support run is already in progress"
  exit 0
fi

today="$(date +%F)"
now="$(date +%s)"
last_ids="" last_run=0 runs_today=0 runs_day="" notice_day=""
if [[ -f "$STATE_FILE" ]]; then
  last_ids="$(jq -r '.lastIds // ""' "$STATE_FILE")"
  last_run="$(jq -r '.lastRun // 0' "$STATE_FILE")"
  runs_day="$(jq -r '.runsDay // ""' "$STATE_FILE")"
  runs_today="$(jq -r '.runsToday // 0' "$STATE_FILE")"
  notice_day="$(jq -r '.noticeDay // ""' "$STATE_FILE")"
fi
[[ "$runs_day" == "$today" ]] || runs_today=0

save_state() {
  jq -n --arg ids "$pending" --argjson run "$now" --arg day "$today" --argjson runs "$runs_today" --arg notice "$notice_day" \
    '{lastIds: $ids, lastRun: $run, runsDay: $day, runsToday: $runs, noticeDay: $notice}' >"$STATE_FILE"
}

if [[ "$pending" == "$last_ids" ]] && (( now - last_run < COOLDOWN_MINUTES * 60 )); then
  why "the same ads were tried $(( (now - last_run) / 60 )) minutes ago; waiting for the cooldown"
  exit 0
fi

if (( runs_today >= MAX_RUNS_PER_DAY )); then
  if [[ "$notice_day" != "$today" ]]; then
    say "daily limit of $MAX_RUNS_PER_DAY support runs reached; waiting until tomorrow"
    notice_day="$today"
    save_state
  fi
  exit 0
fi

# Remember the attempt before it starts, so a crash cannot turn into a loop.
runs_today=$(( runs_today + 1 ))
save_state
say "ads waiting for support: $pending (run $runs_today of $MAX_RUNS_PER_DAY today)"
"$ROOT/scripts/run-support.sh" || say "the support run failed (see above); not trying these ads again for $COOLDOWN_MINUTES minutes"
