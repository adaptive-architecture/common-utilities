#!/bin/bash

dotnet test \
  /p:CollectCoverage=true \
  /p:Threshold=80 \
  /p:ThresholdStat=total \
  /p:CoverletOutputFormat=\"json,lcov\"  \
  /p:CoverletOutput=\"../../coverage/\" \
  /p:MergeWith=\"../../coverage/coverage.json\"
