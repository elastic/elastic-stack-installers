#!/bin/bash

set -euo pipefail

# Download artifacts from Buildkite "Build stack installers" step
echo "+++ Downloading artifacts..."
# it's possible to also use unix style paths for download (users/buildkites/...)
# but it's an experimental feature that needs an agent flag set: https://buildkite.com/docs/agent/v3#experimental-features-normalised-upload-paths
buildkite-agent artifact download 'users\buildkite\esi\bin\out\**\*.msi' . --step "build-${DRA_WORKFLOW}-independent-agent"

OUTPUT_DIR="stack-installers-output"
mkdir "$OUTPUT_DIR"
mv users/buildkite/esi/bin "$OUTPUT_DIR"
chmod -R 777 "$OUTPUT_DIR"

# Create sha512 for msi file(s)
# We can leave it as this loop in order to account for a possible future ARM msi
pushd "${OUTPUT_DIR}/out/elastic"
for f in *.msi; do 
    sha512sum "${f}" > "${f}.sha512"
done

# The defined "artifacts_path" will pick up "stack-installers-output/**/*"

# Check and set trigger id
if [[ -z "${TRIGGER_JOB_ID}" ]]; then
    echo "TRIGGER_JOB_ID is not set, so not setting metadata"
else
    # If a pipeline that triggered this build passes in a "TRIGGER_JOB_ID" env var, that
    # is an indicator that it will want us to set some metadata in that calling build
    # so that it can reference this specific build ID in order to easily download
    # artifacts saved off in this build.
    #
    # This is a much easier way to pull back artifacts from a triggered build than using
    # a Buildkite token that we then have to manage via vault, etc.
    # See: https://forum.buildkite.community/t/how-to-download-artifacts-back-from-triggered-pipeline/3480/2
    echo "Setting metadata for job that trigger this one"
    buildkite-agent meta-data set "triggered_build_id" "$BUILDKITE_BUILD_ID" --job $TRIGGER_JOB_ID
    buildkite-agent meta-data set "triggered_commit_hash" "$BUILDKITE_COMMIT" --job $TRIGGER_JOB_ID
fi


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
# remove -SNAPSHOT style suffix from VERSION
VERSION=${VERSION%-*}
export VERSION
if [[ -z $VERSION ]]; then
    echo "+++ Required version property from ${MANIFEST_URL} was empty: [$VERSION]. Exiting."
    exit 1
fi

if [ "$DRA_WORKFLOW" == "staging" ]; then
    BEATS_MANIFEST_URL=$(curl https://artifacts-"$DRA_WORKFLOW".elastic.co/beats/latest/"$VERSION".json | jq -r '.manifest_url')
else
    BEATS_MANIFEST_URL=$(curl https://artifacts-"$DRA_WORKFLOW".elastic.co/beats/latest/"$VERSION"-SNAPSHOT.json | jq -r '.manifest_url')
fi

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
