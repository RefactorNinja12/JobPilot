#!/usr/bin/env bash
# Rejects commit messages that don't follow Conventional Commits,
# e.g. "feat(profiles): add Skill value object".
set -euo pipefail

msg_file="$1"
subject=$(head -n1 "$msg_file")

pattern='^(feat|fix|test|refactor|ci|chore|docs|perf|build|style|revert)(\([a-z0-9-]+\))?!?: .{1,100}$'

# Allow git-generated merge and revert subjects
if [[ "$subject" =~ ^(Merge|Revert) ]] || [[ "$subject" =~ $pattern ]]; then
  exit 0
fi

echo "✖ Commit message does not follow Conventional Commits:"
echo "    $subject"
echo "  Expected: <type>(<optional scope>): <description>"
echo "  Types:    feat fix test refactor ci chore docs perf build style revert"
exit 1
