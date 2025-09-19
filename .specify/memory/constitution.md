<!--
Sync Impact Report:
- Version change: initial → 1.0.0
- New constitution creation based on common-utilities project characteristics
- Added sections: Library Design Principles, Quality Standards, Development Workflow, Governance
- Follow-up TODOs: None - all placeholders resolved
-->

# Common Utilities Constitution

## Core Principles

### I. Library-First Design
Every feature MUST be implemented as a standalone library with clear boundaries. Libraries MUST be self-contained, independently testable, and have well-defined public APIs. Each library MUST solve a specific cross-cutting concern without organizational dependencies. Libraries MUST be packaged as NuGet packages following the `AdaptArch.Common.Utilities.*` namespace convention.

**Rationale**: Promotes reusability across projects, maintains clean architecture, and reduces coupling between different utility concerns.

### II. Test-Driven Development (NON-NEGOTIABLE)
Tests MUST be written before implementation. The Red-Green-Refactor cycle is strictly enforced: write failing tests, implement minimal code to pass, then refactor. Unit tests MUST achieve comprehensive coverage, and integration tests MUST use TestContainers for external dependencies (Redis, Postgres). Sample projects MUST demonstrate real-world usage but are excluded from test coverage requirements.

**Rationale**: Ensures reliability, maintainability, and serves as living documentation of expected behavior.

### III. Quality-First Standards
Code MUST pass all quality gates before merge. Warnings MUST be treated as errors. Roslynator analyzers MUST be enabled and passing. SonarCloud quality gates MUST pass. Code coverage MUST be maintained at acceptable levels. All projects MUST use consistent build configuration through `Directory.Build.props`.

**Rationale**: Maintains consistent, professional-grade code quality across all utility libraries.

### IV. Modular Specialization
Each library MUST focus on a single domain of utility functionality (encoding, extensions, Redis operations, etc.). Libraries MUST NOT depend on other utility libraries unless absolutely necessary. Cross-cutting concerns MUST be isolated into their own modules. Dependencies MUST be minimal and well-justified.

**Rationale**: Allows consumers to adopt only needed functionality without bloat, reduces dependency trees, and maintains clear separation of concerns.

### V. API Consistency
Public APIs MUST follow consistent patterns across all libraries. Extension methods MUST be preferred for enhancing existing types. Configuration MUST follow standard .NET patterns. Error handling MUST be consistent and predictable. Documentation MUST be comprehensive for public APIs.

**Rationale**: Provides predictable developer experience and reduces learning curve when adopting multiple utility libraries.

## Quality Standards

All code MUST meet the following non-negotiable quality standards:
- Target framework: .NET 9.0 with preview language features enabled
- Warnings treated as errors across all projects
- Roslynator analyzers enabled with strict rules
- SonarCloud quality gate compliance required
- Code coverage reporting via Coverlet in OpenCover and LCOV formats
- Integration tests using TestContainers where applicable
- `InternalsVisibleTo` attributes for test access to internal members

## Development Workflow

Development MUST follow this workflow:
1. **Design**: Create library with clear public API surface
2. **Test**: Write comprehensive unit tests covering all public functionality
3. **Implement**: Write minimal code to pass tests
4. **Integrate**: Add integration tests for external dependencies
5. **Document**: Create sample projects demonstrating usage
6. **Package**: Configure NuGet packaging with appropriate metadata
7. **Validate**: Ensure all quality gates pass before merge

Code reviews MUST verify compliance with all constitutional principles. Complex architectural decisions MUST be documented and justified.

## Governance

This constitution supersedes all other development practices and guidelines. All pull requests MUST be reviewed for constitutional compliance. Any exceptions MUST be explicitly documented and justified.

**Amendment Process**: Constitutional changes require documentation of rationale, impact analysis, and migration plan for affected code. Major principle changes require project-wide review and approval.

**Compliance Review**: Regular audits of libraries against constitutional principles. Non-compliant code MUST be refactored or documented as technical debt with remediation plan.

**Version**: 1.0.0 | **Ratified**: 2025-09-19 | **Last Amended**: 2025-09-19
