#!/bin/bash

set -euo pipefail

# Download artifacts from Buildkite "Build stack installers" step
echo "+++ Downloading artifacts..."
# it's possible to also use unix style paths for download (users/buildkites/...)
# but it's an experimental feature that needs an agent flag set: https://buildkite.com/docs/agent/v3#experimental-features-normalised-upload-paths
buildkite-agent artifact download 'users\buildkite\esi\bin\out\**\*.msi' . --step "build-${DRA_WORKFLOW}-independent-agent"

echo "--- Creating output dir and moving files there"
OUTPUT_DIR="stack-installers-output"
mkdir "$OUTPUT_DIR"
mv users/buildkite/esi/bin "$OUTPUT_DIR"
chmod -R 777 "$OUTPUT_DIR"

echo "--- ls -alR"
ls -alR "${OUTPUT_DIR}"

# Create sha512 for msi file(s)
# We can leave it as this loop in order to account for a possible future ARM msi
echo "--- Creating sha512 files"
pushd "${OUTPUT_DIR}/bin/out/elastic"
for f in *.msi; do 
    sha512sum "${f}" > "${f}.sha512"
done

# The defined "artifacts_path" will pick up "stack-installers-output/**/*"

# Check and set trigger id
if [[ -z "${TRIGGER_JOB_ID-}" ]]; then
    echo "--- TRIGGER_JOB_ID is not set, so not setting metadata"
else
    # If a pipeline that triggered this build passes in a "TRIGGER_JOB_ID" env var, that
    # is an indicator that it will want us to set some metadata in that calling build
    # so that it can reference this specific build ID in order to easily download
    # artifacts saved off in this build.
    #
    # This is a much easier way to pull back artifacts from a triggered build than using
    # a Buildkite token that we then have to manage via vault, etc.
    # See: https://forum.buildkite.community/t/how-to-download-artifacts-back-from-triggered-pipeline/3480/2
    echo "--- Setting metadata for job that trigger this one"
    buildkite-agent meta-data set "triggered_build_id" "$BUILDKITE_BUILD_ID" --job $TRIGGER_JOB_ID
    buildkite-agent meta-data set "triggered_commit_hash" "$BUILDKITE_COMMIT" --job $TRIGGER_JOB_ID
fi
