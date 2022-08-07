#!/bin/bash

dotnet test \
  /p:CollectCoverage=\"true\" \
  /p:CoverletOutputFormat=\"json,lcov\"  \
  /p:CoverletOutput=\"../../coverage/\" \
  /p:MergeWith=\"../../coverage/coverage.json\"
  /p:Exclude=\"[*.IntegrationTests?]*\"

#  /p:Threshold=80 \
#  /p:ThresholdStat=total \
#  /p:Exclude=\"[*.IntegrationTests?]*\" - exclude all types in an assembly ending with "IntegrationTests" or "IntegrationTest" (the ? makes the s optional)
