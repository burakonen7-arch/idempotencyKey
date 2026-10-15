#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH="/workspace/.dotnet:$PATH"
docker compose up -d --wait postgres
# Bu parola yalnızca yerel öğrenme veritabanı içindir.
export ConnectionStrings__Postgres="${ConnectionStrings__Postgres:-Host=localhost;Port=5432;Database=idempotency;Username=learning;Password=local-learning-only}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://127.0.0.1:5080}"
exec dotnet run --no-launch-profile --project src/Idempotency.Api
