#!/bin/bash

set -euo pipefail


# Download artifacts from Buildkite "Build stack installers" step
echo "+++ Downloading artifacts..."
# it's possible to also use unix style paths for download (users/buildkites/...)
# but it's an experimental feature that needs an agent flag set: https://buildkite.com/docs/agent/v3#experimental-features-normalised-upload-paths
buildkite-agent artifact download 'users\buildkite\esi\bin\out\**\*.msi' . --step build-"${DRA_WORKFLOW}"
mv users/buildkite/esi/bin bin
chmod -R 777 bin/out

echo "+++ Setting DRA params" 
# Shared secret path containing the dra creds for project teams
DRA_CREDS=$(vault kv get -field=data -format=json kv/ci-shared/release/dra-role)
VAULT_ADDR=$(echo $DRA_CREDS | jq -r '.vault_addr')
VAULT_ROLE_ID=$(echo $DRA_CREDS | jq -r '.role_id')
VAULT_SECRET_ID=$(echo $DRA_CREDS | jq -r '.secret_id') 
BRANCH="${BUILDKITE_BRANCH}"
export VAULT_ADDR VAULT_ROLE_ID VAULT_SECRET_ID

# Retrieve version value
VERSION=$(curl -s --retry 5 --retry-delay 10 "$MANIFEST_URL" | jq -r '.version')
export VERSION
if [[ -z $VERSION ]]; then
    echo "+++ Required version property from ${MANIFEST_URL} was empty: [$VERSION]. Exiting."
    exit 1
fi

BEATS_MANIFEST_URL=$(curl https://artifacts-"$DRA_WORKFLOW".elastic.co/beats/latest/"$VERSION".json | jq -r '.manifest_url')

# Publish DRA artifacts
function run_release_manager() {
    echo "+++ Publishing $BUILDKITE_BRANCH ${DRA_WORKFLOW} DRA artifacts..."
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
        --branch "${BRANCH}" \
        --commit "${BUILDKITE_COMMIT}" \
        --workflow "${DRA_WORKFLOW}" \
        --version "${VERSION}" \
        --artifact-set main \
        --dependency beats:"${BEATS_MANIFEST_URL}" \
        $dry_run \
        #
}

run_release_manager
RM_EXIT_CODE=$?

exit $RM_EXIT_CODE
