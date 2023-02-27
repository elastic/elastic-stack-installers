#!/bin/bash

set -uo pipefail

set +x

# Download artifacts from Buildkite "Build stack installers" step
echo "+++ Downloading artifacts..."
mkdir -p bin/out
buildkite-agent artifact download 'bin\out\**\*.msi' bin/out/ --step build

# Check if any artifacts were downloaded 
if [ -n "$(find bin/out/ -maxdepth 1 -name '*.msi' -print -quit)" ]
then
  echo "Artifacts downloaded successfully"
else
  echo "No artifacts were downloaded"
  exit 1
fi


echo "+++ Setting DRA params" 

# Shared secret path containing the dra creds for project teams
DRA_CREDS=$(vault kv get -field=data -format=json kv/ci-shared/release/dra-role)
export DRA_CREDS

# Retrieve version value
VERSION=$(cat Directory.Build.props | awk -F'[><]' '/<StackVersion>/{print $3}' | tr -d '[:space:]')
export VERSION

export WORKFLOW="snapshot"

# Publish DRA artifacts for both snapshot and staging
echo "+++ Publishing DRA artifacts..."
    docker run --rm \
    --name release-manager \
    -e VAULT_ADDR="$(echo "$DRA_CREDS" | jq -r '.vault_addr')" \
    -e VAULT_ROLE_ID="$(echo "$DRA_CREDS" | jq -r '.role_id')" \
    -e VAULT_SECRET_ID="$(echo "$DRA_CREDS" | jq -r '.secret_id')" \
    --mount type=bind,readonly=false,src="$PWD/artifacts",target=<Your/Projects/Build/Dir> \   # The directory containing the projects artifacts
    docker.elastic.co/infra/release-manager:latest \
        cli collect \
        --project elastic-stack-installers \
        --branch "$BUILDKITE_BRANCH" \
        --commit "$BUILDKITE_COMMIT" \
        --workflow "$WORKFLOW" \
        --version "$VERSION" \
        --artifact-set main \
        --dry-run

# I can run snapshot on main+both on version branches and use a matrix step.