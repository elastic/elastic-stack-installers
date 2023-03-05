#!/bin/bash

set -uo pipefail

set +x

# Download artifacts from Buildkite "Build stack installers" step
echo "+++ Downloading artifacts..."
buildkite-agent artifact download 'bin\out\**\*.msi' "$PWD"/bin/out/ --step build
ls -laR "$PWD"/bin/out
# mv bin/out/*/*.msi bin/out/.

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

# Publish DRA artifacts for both snapshot and staging

function run_release_manager() {
    echo "+++ Publishing $BUILDKITE_BRANCH ${WORKFLOW} DRA artifacts..."
    docker run --rm \
        --name release-manager \
        -e VAULT_ADDR="${VAULT_ADDR}" \
        -e VAULT_ROLE_ID="${VAULT_ROLE_ID}" \
        -e VAULT_SECRET_ID="${VAULT_SECRET_ID}" \
        --mount type=bind,readonly=false,src="${PWD}",target=/artifacts \
        docker.elastic.co/infra/release-manager:latest \
        cli collect \
        --project elastic-stack-installers \
        --branch main \
        --commit "${BUILDKITE_COMMIT}" \
        --workflow "${WORKFLOW}" \
        --version "${VERSION}" \
        --artifact-set main \
        --dry-run
}

export WORKFLOW="snapshot"
run_release_manager
# if [[ $BUILDKITE_BRANCH == "main" ]]; then
#     WORKFLOW="snapshot"
#     run_release_manager
# else
#     WORKFLOW="staging"
#     run_release_manager
#     WORKFLOW="snapshot"
#     run_release_manager
# fi


# I can run snapshot on main+both on version branches and use a matrix step.