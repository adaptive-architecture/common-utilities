#!/bin/bash

rm -rf ./coverage/*
rm -rf ./test/TestResults

dotnet test \
  --nologo \
  --filter \"FullyQualifiedName!~AdaptArch.Common.Utilities.Samples\" \
  /p:CollectCoverage=\"true\" \
  /p:CoverletOutputFormat=\"json,lcov,opencover\"  \
  /p:CoverletOutput=\"../../coverage/\" \
  /p:MergeWith=\"../../coverage/coverage.json\"

#  /p:Threshold=80 \
#  /p:ThresholdStat=total \
