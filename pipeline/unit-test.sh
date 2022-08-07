#!/bin/bash

dotnet test \
  --filter \"FullyQualifiedName!~IntegrationTests\" \
  /p:CollectCoverage=\"true\" \
  /p:CoverletOutputFormat=\"json,lcov\"  \
  /p:CoverletOutput=\"../../coverage/\" \
  /p:MergeWith=\"../../coverage/coverage.json\"

#  /p:Threshold=80 \
#  /p:ThresholdStat=total \
