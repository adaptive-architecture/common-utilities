dotnet test ./test/Common.Utilities.UnitTests/Common.Utilities.UnitTests.csproj `
  --no-restore `
  --configuration Debug `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=lcov `
  /p:CoverletOutput=../../coverage/lcov.info `
