#!/bin/bash

BRANCH="${BRANCH:-$BUILDKITE_BRANCH}"

BEATS_MANIFEST=$(curl -s https://artifacts-staging.elastic.co/beats/latest/$BRANCH.json | jq -r '.manifest_url')
INSTALLERS_MANIFEST=$(curl -s  https://artifacts-staging.elastic.co/elastic-stack-installers/latest/$BRANCH.json | jq -r '.manifest_url')
INSTALLERS_BEATS_DEPENDENCY=$(curl -s $INSTALLERS_MANIFEST | jq -r '.projects."elastic-stack-installers".dependencies[] | select(.prefix == "beats") | .build_uri')

if [ "$BEATS_MANIFEST" = "$INSTALLERS_BEATS_DEPENDENCY" ]
then
   echo "We have the latest beats! Nothing to do" >&2
   echo "steps: []"
else
   echo "Need to trigger a build, $BEATS_MANIFEST available but we have $INSTALLERS_BEATS_DEPENDENCY" >&2
   cat .buildkite/trigger.yml 
fi

