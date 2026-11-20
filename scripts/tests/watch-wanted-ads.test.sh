#!/usr/bin/env bash
# Tests scripts/watch-wanted-ads.sh without spending anything: a fake `claude`, and a local web server that stands in
# for the Jobpilot API and MCP server. Run: scripts/tests/watch-wanted-ads.test.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WORK="$(mktemp -d)"
SERVER_PID=""
cleanup() { [[ -n "$SERVER_PID" ]] && kill "$SERVER_PID" 2>/dev/null; rm -rf "$WORK"; }
trap cleanup EXIT

mkdir -p "$WORK/www/api/jobs" "$WORK/state"
echo '{}' >"$WORK/www/mcp"
PORT="$(python3 -I -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1])')"
(cd "$WORK/www" && exec python3 -I -m http.server "$PORT" --bind 127.0.0.1 >/dev/null 2>&1) &
SERVER_PID=$!
for _ in $(seq 50); do curl -s -o /dev/null -m 1 "http://127.0.0.1:$PORT/mcp" && break; sleep 0.1; done

# A fake claude that records that it was started and answers like the real one.
cat >"$WORK/claude" <<FAKE
#!/usr/bin/env bash
echo "called" >>"$WORK/calls.log"
echo '{"result":"ok","is_error":false,"total_cost_usd":0,"num_turns":1,"usage":{}}'
FAKE
chmod +x "$WORK/claude"

export JOBPILOT_ENV_FILE="$WORK/no-env" XDG_STATE_HOME="$WORK/state" CLAUDE_BIN="$WORK/claude"
export JOBPILOT_MCP_URL="http://127.0.0.1:$PORT/mcp" JOBPILOT_WEB_URL="http://127.0.0.1:$PORT"
export JOBPILOT_WATCH_COOLDOWN_MINUTES=60 JOBPILOT_WATCH_MAX_RUNS_PER_DAY=6 JOBPILOT_WATCH_VERBOSE=1

failures=0
calls() { [[ -f "$WORK/calls.log" ]] && wc -l <"$WORK/calls.log" || echo 0; }
ads() { # ads "id:interested:hasSupport" ...  writes the API answer
  local json="[" first=true spec
  for spec in "$@"; do
    IFS=: read -r id interested support <<<"$spec"
    $first || json+=","; first=false
    json+="{\"adId\":\"$id\",\"interested\":$interested,\"hasSupport\":$support}"
  done
  echo "$json]" >"$WORK/www/api/jobs/assessed"
}
watch() { "$ROOT/scripts/watch-wanted-ads.sh" >"$WORK/out.log" 2>&1 || { echo "  watcher exited with an error:"; cat "$WORK/out.log"; failures=$((failures + 1)); }; }
expect_calls() { # expect_calls N "what"
  local actual; actual="$(calls)"
  if [[ "$actual" == "$1" ]]; then echo "ok   $2"; else echo "FAIL $2 (claude started $actual times, expected $1)"; cat "$WORK/out.log"; failures=$((failures + 1)); fi
}

ads "1:true:true" "2:false:false" "3:false:true"
watch; expect_calls 0 "nothing is started when no marked ad lacks support"

ads "1:true:false" "2:false:false"
watch; expect_calls 1 "a marked ad without support starts Claude"

watch; expect_calls 1 "the same pending ads are not tried again within the cooldown"

ads "1:true:false" "2:true:false"
watch; expect_calls 2 "a newly marked ad starts Claude again at once"

JOBPILOT_WATCH_COOLDOWN_MINUTES=0 watch; expect_calls 3 "the same ads are tried again when the cooldown has passed"

( exec 8>"$WORK/state/jobpilot/lock-support"; flock -n 8; ads "9:true:false"; watch; ); expect_calls 3 "nothing is started while a support run is in progress"

ads "4:true:false"
JOBPILOT_WATCH_MAX_RUNS_PER_DAY=4 watch; expect_calls 4 "the fourth run of the day is allowed"
ads "5:true:false"
JOBPILOT_WATCH_MAX_RUNS_PER_DAY=4 watch; expect_calls 4 "no more runs once the daily limit is reached"
grep -q "daily limit" "$WORK/out.log" && echo "ok   the daily limit is said once" || { echo "FAIL the daily limit was not mentioned"; failures=$((failures + 1)); }
JOBPILOT_WATCH_MAX_RUNS_PER_DAY=4 watch
grep -q "daily limit" "$WORK/out.log" && { echo "FAIL the daily limit was repeated"; failures=$((failures + 1)); } || echo "ok   the daily limit is not repeated every minute"

kill "$SERVER_PID"; SERVER_PID=""; sleep 0.3
ads "6:true:false" 2>/dev/null || true
JOBPILOT_WATCH_MAX_RUNS_PER_DAY=99 watch; expect_calls 4 "an unreachable web API is not an error and starts nothing"

(( failures == 0 )) && echo "All good." || { echo "$failures failure(s)."; exit 1; }
