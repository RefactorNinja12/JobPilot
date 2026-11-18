#!/usr/bin/env bash
# Builds Jobpilot and runs it in a local k3d cluster named "jobpilot".
# Safe to run again: it reuses the cluster and the database password, and rolls out new images.
set -euo pipefail

CLUSTER=jobpilot
CONTEXT="k3d-$CLUSTER"
NAMESPACE=jobpilot
# Tailscale Operator chart, pinned so a redeploy never upgrades it silently. Bump it deliberately.
TAILSCALE_CHART_VERSION="${TAILSCALE_CHART_VERSION:-1.104.7}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
kc() { kubectl --context "$CONTEXT" "$@"; } # never touch another cluster by accident

for tool in docker k3d kubectl openssl; do
  command -v "$tool" >/dev/null || { echo "Missing $tool" >&2; exit 1; }
done

if ! k3d cluster list "$CLUSTER" >/dev/null 2>&1; then
  echo "==> Creating k3d cluster $CLUSTER"
  k3d cluster create --config "$ROOT/deploy/k3d/cluster.yaml" --wait
elif ! k3d cluster list "$CLUSTER" -o json | grep -q '"serversRunning":1'; then
  echo "==> Starting k3d cluster $CLUSTER"
  k3d cluster start "$CLUSTER" --wait
fi

echo "==> Building images"
docker build --target worker -t jobpilot-worker:local "$ROOT"
docker build --target mcp -t jobpilot-mcp:local "$ROOT"
docker build --target web -t jobpilot-web:local "$ROOT"

echo "==> Importing images into the cluster"
k3d image import jobpilot-worker:local jobpilot-mcp:local jobpilot-web:local --cluster "$CLUSTER"

kc apply -f "$ROOT/deploy/k8s/namespace.yaml"

# The database password is generated once and kept in the cluster; it is never written to the repo.
if ! kc -n "$NAMESPACE" get secret jobpilot-db >/dev/null 2>&1; then
  echo "==> Creating database secret"
  password="$(openssl rand -hex 24)"
  kc -n "$NAMESPACE" create secret generic jobpilot-db \
    --from-literal=POSTGRES_PASSWORD="$password" \
    --from-literal=connection-string="Host=postgres;Port=5432;Database=jobpilot;Username=jobpilot;Password=$password"
fi

# The Tailscale Operator puts the MCP server on the tailnet. It needs OAuth credentials in the
# tailscale/operator-oauth Secret, which the user creates (see README); without it this step is skipped.
if kc -n tailscale get secret operator-oauth >/dev/null 2>&1; then
  command -v helm >/dev/null || { echo "Missing helm (needed for the Tailscale Operator)" >&2; exit 1; }
  echo "==> Installing the Tailscale Operator"
  helm repo add tailscale https://pkgs.tailscale.com/helmcharts --force-update >/dev/null
  helm repo update tailscale >/dev/null
  # No oauth values: the chart reads the pre-created operator-oauth Secret.
  helm upgrade --install tailscale-operator tailscale/tailscale-operator \
    --kube-context "$CONTEXT" --namespace tailscale \
    --version "$TAILSCALE_CHART_VERSION" --wait
else
  echo "==> Skipping the Tailscale Operator: no tailscale/operator-oauth Secret (see README)"
fi

echo "==> Applying manifests"
kc apply -k "$ROOT/deploy/k8s"

# Same image tags on every build, so restart to pick up the new images.
kc -n "$NAMESPACE" rollout restart deployment/worker deployment/mcp deployment/web
kc -n "$NAMESPACE" rollout status statefulset/postgres --timeout=180s
kc -n "$NAMESPACE" rollout status deployment/worker --timeout=180s
kc -n "$NAMESPACE" rollout status deployment/mcp --timeout=180s
kc -n "$NAMESPACE" rollout status deployment/web --timeout=180s

cat <<MSG

Jobpilot is running in k3d cluster "$CLUSTER".
  MCP server:  kubectl --context $CONTEXT -n $NAMESPACE port-forward svc/mcp 5199:80
               then http://localhost:5199/mcp (the "jobpilot" entry in .mcp.json)
  Over Tailscale (when the operator is installed): https://jobpilot.<tailnet>.ts.net/mcp
               the address is in: kubectl --context $CONTEXT -n $NAMESPACE get ingress mcp
  Web UI:      kubectl --context $CONTEXT -n $NAMESPACE port-forward svc/web 5299:80
               then http://localhost:5299
  Over Tailscale: https://jobpilot-web.<tailnet>.ts.net
               the address is in: kubectl --context $CONTEXT -n $NAMESPACE get ingress web
  Worker log:  kubectl --context $CONTEXT -n $NAMESPACE logs deploy/worker -f
MSG
