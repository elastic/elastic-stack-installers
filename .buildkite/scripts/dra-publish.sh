#!/bin/bash

set -euo pipefail

set +x

# Download artifacts from Buildkite "Build stack installers" step
echo "+++ Downloading artifacts..."
buildkite-agent artifact download 'bin\out\**\*.msi' . --step build-"${WORKFLOW}"
chmod -R 777 bin/out

echo "+++ Setting DRA params" 

# Shared secret path containing the dra creds for project teams
DRA_CREDS=$(vault kv get -field=data -format=json kv/ci-shared/release/dra-role)
VAULT_ADDR=$(echo $DRA_CREDS | jq -r '.vault_addr')
VAULT_ROLE_ID=$(echo $DRA_CREDS | jq -r '.role_id')
VAULT_SECRET_ID=$(echo $DRA_CREDS | jq -r '.secret_id') 
export VAULT_ADDR VAULT_ROLE_ID VAULT_SECRET_ID

# Retrieve version value
VERSION=$(cat Directory.Build.props | awk -F'[><]' '/<StackVersion>/{print $3}' | tr -d '[:space:]')
export VERSION

# Publish DRA artifacts
function run_release_manager() {
    echo "+++ Publishing $BUILDKITE_BRANCH ${WORKFLOW} DRA artifacts..."
    set -x # Enable command tracing
    dry_run=""
    if [ "$BUILDKITE_PULL_REQUEST" != "false" ]; then
        dry_run="--dry-run"
    fi
    docker run --rm \
        --name release-manager \
        -e VAULT_ADDR="${VAULT_ADDR}" \
        -e VAULT_ROLE_ID="${VAULT_ROLE_ID}" \
        -e VAULT_SECRET_ID="${VAULT_SECRET_ID}" \
        --mount type=bind,readonly=false,src="${PWD}",target=/artifacts \
        docker.elastic.co/infra/release-manager:latest \
        cli collect \
        --project elastic-stack-installers \
        --branch "7.17" \
        --commit "${BUILDKITE_COMMIT}" \
        --workflow "${WORKFLOW}" \
        --version "${VERSION}" \
        --artifact-set main \
        $dry_run \
        #
    set +x # Disable command tracing
}

run_release_manager

# stacking manifest https://artifacts-staging.elastic.co/beats/latest/8.6.3.json manifest_url
# snapshot manifest https://artifacts-api.elastic.co/v1/versions/8.6.3-SNAPSHOT/builds/latest/projects/beats external_artifacts_manifest_url
