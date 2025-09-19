# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, quality tools (Roslynator, SonarCloud)
   → Tests: contract tests, integration tests (TestContainers if needed)
   → Core: models, services, extension methods
   → Library: public API, NuGet package configuration
   → Polish: comprehensive unit tests, performance validation, documentation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **.NET Library**: `src/[LibraryName]/`, `test/[LibraryName].Tests/`
- **Multi-library solution**: `src/[Module1]/`, `src/[Module2]/`, `test/[Module1].Tests/`
- **Sample projects**: `samples/[SampleName]/`
- Paths shown below assume .NET library structure - adjust based on plan.md structure

## Phase 3.1: Setup
- [ ] T001 Create .NET library project structure per implementation plan
- [ ] T002 Initialize .csproj with target framework (.NET 9.0) and dependencies
- [ ] T003 [P] Configure Roslynator analyzers and Directory.Build.props
- [ ] T004 [P] Configure NuGet package metadata and versioning

## Phase 3.2: Tests First (TDD) ⚠️ NON-NEGOTIABLE - MUST COMPLETE BEFORE 3.3
**CRITICAL: Constitution Principle II - Tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T005 [P] Unit test for [PublicClass] in test/[Library].Tests/[PublicClass]Tests.cs
- [ ] T006 [P] Unit test for [ExtensionMethods] in test/[Library].Tests/[Extensions]Tests.cs
- [ ] T007 [P] Integration test with TestContainers (if external deps) in test/[Library].Tests/Integration/[Feature]IntegrationTests.cs
- [ ] T008 [P] Contract test for public API surface in test/[Library].Tests/ApiContractTests.cs

## Phase 3.3: Core Implementation (ONLY after tests are failing)
- [ ] T009 [P] Core model/entity in src/[Library]/[Entity].cs
- [ ] T010 [P] Extension methods in src/[Library]/Extensions/[Type]Extensions.cs
- [ ] T011 [P] Service implementation in src/[Library]/[Service].cs
- [ ] T012 Public API classes in src/[Library]/[PublicClass].cs
- [ ] T013 Input validation and error handling
- [ ] T014 Configure InternalsVisibleTo for test access

## Phase 3.4: Library Integration
- [ ] T015 Configure dependency injection extensions (if applicable)
- [ ] T016 Add configuration options following .NET patterns
- [ ] T017 Implement logging and diagnostics
- [ ] T018 Add XML documentation for public API

## Phase 3.5: Quality & Packaging
- [ ] T019 [P] Comprehensive unit test coverage in test/[Library].Tests/
- [ ] T020 Performance benchmarks (if applicable)
- [ ] T021 [P] Create sample project in samples/[Library].Sample/
- [ ] T022 Verify all quality gates pass (Roslynator, SonarCloud)
- [ ] T023 Validate NuGet package builds correctly

## Dependencies
- Setup (T001-T004) before all other phases
- Tests (T005-T008) before implementation (T009-T014) - NON-NEGOTIABLE
- Core implementation blocks library integration
- Quality validation (T022) blocks packaging (T023)

## Parallel Example
```
# Launch T005-T008 together (TDD phase):
Task: "Unit test for EncodingService in test/Common.Utilities.Tests/EncodingServiceTests.cs"
Task: "Unit test for StringExtensions in test/Common.Utilities.Tests/StringExtensionsTests.cs"
Task: "Integration test with TestContainers in test/Common.Utilities.Tests/Integration/RedisIntegrationTests.cs"
Task: "Contract test for public API in test/Common.Utilities.Tests/ApiContractTests.cs"
```

## Notes
- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts

## Task Generation Rules
*Applied during main() execution*

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
   
2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks
   
3. **From User Stories**:
   - Each story → integration test [P]
   - Quickstart scenarios → validation tasks

4. **Ordering**:
   - Setup → Tests → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

## Validation Checklist
*GATE: Checked by main() before returning*

- [ ] All public API members have corresponding tests
- [ ] All core entities/services have implementation tasks
- [ ] All tests come before implementation (Constitution Principle II)
- [ ] Parallel tasks truly independent (different .cs files)
- [ ] Each task specifies exact file path with .cs extension
- [ ] No task modifies same file as another [P] task

## Constitution Compliance
*GATE: Verify adherence to Common Utilities Constitution v1.0.0*

- [ ] TDD approach strictly enforced (Principle II - NON-NEGOTIABLE)
- [ ] Library designed as standalone with clear boundaries (Principle I)
- [ ] Quality gates configured (Roslynator, SonarCloud) (Principle III)
- [ ] Single domain focus maintained (Principle IV)
- [ ] API follows consistent .NET patterns (Principle V)
