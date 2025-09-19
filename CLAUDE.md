# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
- `dotnet build` - Build all projects in the solution
- `dotnet build --no-incremental` - Build without incremental building (used in CI)

### Test
- `dotnet test` - Run all tests
- `sh ./pipeline/unit-test.sh` - Run unit tests with coverage collection (preferred method)
- `dotnet test --filter "FullyQualifiedName!~AdaptArch.Common.Utilities.Samples"` - Run tests excluding samples

### Restore
- `dotnet restore` - Restore NuGet packages

## Architecture

This is a .NET 9 solution containing multiple utility libraries packaged as NuGet packages under the `AdaptArch.Common.Utilities` namespace.

### Project Structure
- `src/` - Source projects that produce NuGet packages
- `test/` - Unit and integration test projects
- `samples/` - Sample projects demonstrating usage
- `pipeline/` - Build and deployment scripts

### Core Utilities (`Common.Utilities`)
- **Encoding**: Base32, Base64Url encoders
- **Extensions**: DateTime, Dictionary, Task, JSON, Exception extensions
- **Synchronization**: ExclusiveAccess for thread-safe operations

### Specialized Utilities
- **AspNetCore**: HttpContext extensions
- **Redis**: Message hub (pub/sub), leader election service, lease store
- **Postgres**: Database utilities and extensions
- **Configuration**: Configuration management utilities
- **Hosting**: Hosting-related utilities
- **xUnit**: Testing utilities and extensions

### Key Design Patterns
- All projects use `Directory.Build.props` for shared build configuration
- `InternalsVisibleTo` attributes expose internals to corresponding test projects
- Solution uses the modern .slnx format
- Consistent naming: `AdaptArch.Common.Utilities.<Module>`
- Target framework: .NET 9.0 with preview language features
- Warnings treated as errors with Roslynator analyzers

### Integration Tests
Redis and Postgres modules include integration tests that use TestContainers. The `TESTCONTAINERS_RYUK_DISABLED=true` environment variable is set in CI environments.

### Coverage and Quality
- Uses Coverlet for code coverage collection
- SonarCloud integration for code quality analysis
- Coverage reports generated in OpenCover and LCOV formats
