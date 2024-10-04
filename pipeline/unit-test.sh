#!/bin/bash

# in case the CI environment varialbe has a non-empty value ignore the "WindowOnly" tests
if [ -n "$CI" ]; then
  echo "Disabling TESTCONTAINERS RUYK"
  export TESTCONTAINERS_RYUK_DISABLED=true
fi

rm -rf ./coverage/*
rm -rf ./test/TestResults

dotnet test \
  --nologo \
  --filter \"FullyQualifiedName!~AdaptArch.Common.Utilities.Samples\" \
  -p:CollectCoverage=\"true\" \
  -p:CoverletOutputFormat=\"json,lcov,opencover\"  \
  -p:CoverletOutput=\"../../coverage/\" \
  -p:MergeWith=\"../../coverage/coverage.json\"


#  -p:Threshold=80 \
#  -p:ThresholdStat=total \
#  --logger "console;verbosity=normal" \
