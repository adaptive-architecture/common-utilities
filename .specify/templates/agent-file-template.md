# Common Utilities Development Guidelines

Auto-generated from all feature plans. Last updated: [DATE]

## Constitutional Principles

This project follows the Common Utilities Constitution v1.0.0. All development MUST adhere to:

1. **Library-First Design** - Standalone libraries with clear boundaries, packaged as NuGet packages
2. **Test-Driven Development (NON-NEGOTIABLE)** - Red-Green-Refactor cycle strictly enforced
3. **Quality-First Standards** - Warnings as errors, Roslynator analyzers, SonarCloud compliance
4. **Modular Specialization** - Single domain focus, minimal dependencies
5. **API Consistency** - Standard .NET patterns, extension methods preferred

## Active Technologies
- **.NET 9.0** with preview language features
- **C#** with modern language features enabled
- **xUnit** for unit and integration testing
- **TestContainers** for integration tests with external dependencies
- **Coverlet** for code coverage (OpenCover and LCOV formats)
- **Roslynator** analyzers for code quality
- **SonarCloud** for quality gate compliance

[EXTRACTED FROM ALL PLAN.MD FILES - ADD ADDITIONAL TECHNOLOGIES AS NEEDED]

## Project Structure
```
src/
├── Common.Utilities/                    # Core utilities
├── Common.Utilities.AspNetCore/         # ASP.NET Core extensions
├── Common.Utilities.Configuration/      # Configuration utilities
├── Common.Utilities.Hosting/            # Hosting utilities
├── Common.Utilities.Postgres/          # PostgreSQL utilities
├── Common.Utilities.Redis/             # Redis utilities
└── Common.Utilities.xUnit/             # xUnit testing utilities

test/
├── Common.Utilities.Tests/
├── Common.Utilities.AspNetCore.Tests/
└── [corresponding test projects for each library]

samples/
├── [LibraryName].Sample/               # Demonstration projects
└── [usage examples]

pipeline/                               # Build and deployment scripts
```

[ACTUAL STRUCTURE FROM PLANS - UPDATE AS FEATURES ARE ADDED]

## Commands

### Build & Test
- `dotnet build` - Build all projects in the solution
- `dotnet build --no-incremental` - Build without incremental building (used in CI)
- `dotnet test` - Run all tests
- `sh ./pipeline/unit-test.sh` - Run unit tests with coverage collection (preferred)
- `dotnet test --filter "FullyQualifiedName!~AdaptArch.Common.Utilities.Samples"` - Run tests excluding samples

### Package Management
- `dotnet restore` - Restore NuGet packages
- `dotnet pack` - Create NuGet packages
- `dotnet nuget push` - Publish packages (CI/CD only)

### Quality & Analysis
- Roslynator analyzers run automatically during build
- SonarCloud analysis runs in CI pipeline
- Code coverage collected via Coverlet

[ONLY COMMANDS FOR ACTIVE TECHNOLOGIES - UPDATE AS NEEDED]

## Code Style

### C# Conventions
- **Target Framework**: .NET 9.0
- **Language Version**: Preview features enabled
- **Nullable Reference Types**: Enabled
- **Warnings as Errors**: Enforced via Directory.Build.props
- **Analyzers**: Roslynator rules enabled with strict enforcement

### Library Design Patterns
- **Extension Methods**: Preferred for enhancing existing types
- **Dependency Injection**: Use standard .NET DI patterns
- **Configuration**: Follow IOptions<T> patterns
- **Error Handling**: Consistent exception types and messages
- **Async/Await**: Use consistently for I/O operations

### Testing Conventions
- **TDD Approach**: Tests written before implementation (NON-NEGOTIABLE)
- **Test Organization**: One test class per public class
- **Integration Tests**: Use TestContainers for external dependencies
- **Test Naming**: [MethodName]_[Scenario]_[ExpectedResult]
- **Assertions**: Use xUnit assertion methods

### Public API Design
- **InternalsVisibleTo**: Used for test access to internal members
- **XML Documentation**: Required for all public APIs
- **Semantic Versioning**: MAJOR.MINOR.PATCH for NuGet packages
- **Namespace Convention**: AdaptArch.Common.Utilities.[Module]

[LANGUAGE-SPECIFIC GUIDELINES FOR ANY ADDITIONAL TECHNOLOGIES]

## Quality Gates

All code MUST pass these gates before merge:

- [ ] Build succeeds without warnings
- [ ] All tests pass (unit and integration)
- [ ] Code coverage meets project standards
- [ ] Roslynator analyzers pass
- [ ] SonarCloud quality gate passes
- [ ] Constitutional principles verified

## Recent Changes
[LAST 3 FEATURES AND WHAT THEY ADDED - UPDATE AS FEATURES ARE IMPLEMENTED]

## Integration Guidelines

### Adding New Libraries
1. Create library following namespace convention
2. Add corresponding test project with InternalsVisibleTo
3. Configure NuGet package metadata
4. Create sample project demonstrating usage
5. Update solution file and build configuration

### External Dependencies
- Minimize dependencies per Constitutional Principle IV
- Use TestContainers for integration testing external services
- Document dependency rationale in design docs

<!-- MANUAL ADDITIONS START -->
<!-- Add project-specific guidelines, exceptions, or additional context here -->
<!-- This section is preserved during auto-generation -->
<!-- MANUAL ADDITIONS END -->
