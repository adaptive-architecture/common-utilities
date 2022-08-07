#!/bin/bash

dotnet test \
  /p:CollectCoverage=\"true\" \
  /p:CoverletOutputFormat=\"json,lcov\"  \
  /p:CoverletOutput=\"../../coverage/\" \
  /p:MergeWith=\"../../coverage/coverage.json\"

#  --filter \"FullyQualifiedName!~IntegrationTests\" \

#  /p:Threshold=80 \
#  /p:ThresholdStat=total \
