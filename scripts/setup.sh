#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH="/workspace/.dotnet:$PATH"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
if ! command -v dotnet >/dev/null; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/idempotency-dotnet-install.sh
  bash /tmp/idempotency-dotnet-install.sh --version 10.0.100 --install-dir /workspace/.dotnet
fi
dotnet restore --locked-mode
dotnet build --no-restore
