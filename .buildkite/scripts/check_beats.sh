#!/bin/bash

BRANCH="${BRANCH:-$BUILDKITE_BRANCH}"

BEATS_MANIFEST=$(curl  https://artifacts-staging.elastic.co/beats/latest/$BRANCH.json | jq -r '.manifest_url')
INSTALLERS_MANIFEST=$(curl  https://artifacts-staging.elastic.co/elastic-stack-installers/latest/$BRANCH.json | jq -r '.manifest_url')
INSTALLERS_BEATS_DEPENDENCY=$(curl $INSTALLERS_MANIFEST | jq -r '.projects.elasticsearch.dependencies[] | select(.prefix == "beats") | .build_uri')

if [ "$BEATS_MANIFEST" = "$INSTALLERS_BEATS_DEPENDENCY" ]
then
   echo "We have the latest beats! Nothing to do" >&2
else
   echo "Need to trigger a build, $BEATS_MANIFEST available but ES has $INSTALLERS_BEATS_DEPENDENCY" > &2
fi

echo "steps:"
