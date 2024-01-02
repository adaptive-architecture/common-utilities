#!/bin/bash

rm -rf ./coverage/*
rm -rf ./test/TestResults

dotnet test \
  /p:CollectCoverage=\"true\" \
  /p:CoverletOutputFormat=\"json,lcov,opencover\"  \
  /p:CoverletOutput=\"../../coverage/\" \
  /p:MergeWith=\"../../coverage/coverage.json\"

#  --filter \"FullyQualifiedName!~IntegrationTests\" \

#  /p:Threshold=80 \
#  /p:ThresholdStat=total \
