# Tasks: Version-Aware ConsistentHashing.HashRing

**Input**: Design documents from `C:\Users\ValentinDide\code\github.com\adaptive-architecture\common-utilities\specs\003-i-would-like\`
**Prerequisites**: plan.md, research.md, data-model.md, contracts/VersionAwareHashRing-API.cs, quickstart.md

## Execution Flow (main)
```
1. Load plan.md from feature directory → Extract: C# .NET 9.0, xUnit testing, existing ConsistentHashing namespace
2. Load design documents:
   → data-model.md: ConfigurationSnapshot<T>, HistoryManager<T>, ServerCandidateResult<T>
   → contracts/: HashRingOptions extension, new API methods
   → quickstart.md: Integration test scenarios for migration workflows
3. Generate tasks by category:
   → Setup: Extension to existing ConsistentHashing project
   → Tests: Version-aware API tests, migration scenario tests
   → Core: Internal entities, history management, version-aware lookup
   → Public API: New methods on HashRing<T>, exception types
   → Polish: Thread-safety tests, performance validation, samples
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same HashRing.cs file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Phase 3.1: Setup
- [ ] **T001** Verify existing ConsistentHashing project structure and dependencies
- [ ] **T002** [P] Update `src/Common.Utilities/Common.Utilities.csproj` to include new internal entities
- [ ] **T003** [P] Ensure test project references for version-aware features in `test/Common.Utilities.UnitTests/Common.Utilities.UnitTests.csproj`

## Phase 3.2: Tests First (TDD) ⚠️ NON-NEGOTIABLE - MUST COMPLETE BEFORE 3.3
**CRITICAL: Constitution Principle II - Tests MUST be written and MUST FAIL before ANY implementation**

### Contract Tests [P]
- [ ] **T004** [P] Version-aware HashRingOptions contract test in `test/Common.Utilities.UnitTests/ConsistentHashing/HashRingOptionsVersionTests.cs`
- [ ] **T005** [P] ServerCandidateResult<T> contract test in `test/Common.Utilities.UnitTests/ConsistentHashing/ServerCandidateResultTests.cs`
- [ ] **T006** [P] HashRingHistoryLimitExceededException contract test in `test/Common.Utilities.UnitTests/ConsistentHashing/HashRingHistoryLimitExceededExceptionTests.cs`

### Core Entity Tests [P]
- [ ] **T007** [P] ConfigurationSnapshot<T> unit test in `test/Common.Utilities.UnitTests/ConsistentHashing/ConfigurationSnapshotTests.cs`
- [ ] **T008** [P] HistoryManager<T> unit test in `test/Common.Utilities.UnitTests/ConsistentHashing/HistoryManagerTests.cs`

### Version-Aware HashRing API Tests
- [ ] **T009** Version-aware HashRing<T> extension methods test in `test/Common.Utilities.UnitTests/ConsistentHashing/HashRingVersionAwareTests.cs`

### Integration Tests [P]
- [ ] **T010** [P] Migration scenario integration test (2→3 servers) in `test/Common.Utilities.UnitTests/ConsistentHashing/MigrationScenarioTests.cs`
- [ ] **T011** [P] History limit enforcement integration test in `test/Common.Utilities.UnitTests/ConsistentHashing/HistoryLimitIntegrationTests.cs`
- [ ] **T012** [P] Thread-safety integration test in `test/Common.Utilities.UnitTests/ConsistentHashing/VersionAwareConcurrencyTests.cs`
- [ ] **T013** [P] Performance baseline comparison test in `test/Common.Utilities.UnitTests/ConsistentHashing/VersionAwarePerformanceTests.cs`

## Phase 3.3: Core Implementation (ONLY after tests are failing)

### Internal Entities [P]
- [ ] **T014** [P] ConfigurationSnapshot<T> implementation in `src/Common.Utilities/ConsistentHashing/ConfigurationSnapshot.cs`
- [ ] **T015** [P] HistoryManager<T> implementation in `src/Common.Utilities/ConsistentHashing/HistoryManager.cs`

### Public API Classes [P]
- [ ] **T016** [P] ServerCandidateResult<T> implementation in `src/Common.Utilities/ConsistentHashing/ServerCandidateResult.cs`
- [ ] **T017** [P] HashRingHistoryLimitExceededException implementation in `src/Common.Utilities/ConsistentHashing/HashRingHistoryLimitExceededException.cs`

### HashRing Extensions (Sequential - Same File)
- [ ] **T018** Extend HashRingOptions with version history properties in `src/Common.Utilities/ConsistentHashing/HashRingOptions.cs`
- [ ] **T019** Add version history fields to HashRing<T> in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T020** Implement CreateConfigurationSnapshot method in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T021** Implement ClearHistory method in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T022** Implement GetServerCandidates method in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T023** Implement TryGetServerCandidates method in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T024** Implement GetServerCandidates with maxCandidates method in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T025** Add version history properties (IsVersionHistoryEnabled, HistoryCount, MaxHistorySize) in `src/Common.Utilities/ConsistentHashing/HashRing.cs`

### Thread Safety and Validation
- [ ] **T026** Implement thread-safe history management in `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- [ ] **T027** Add input validation for version-aware methods in `src/Common.Utilities/ConsistentHashing/HashRing.cs`

## Phase 3.4: Library Integration
- [ ] **T028** Update InternalsVisibleTo in `src/Common.Utilities/Common.Utilities.csproj` and `src/Common.Utilities/ConsistentHashing/AssemblyInfo.cs` for test access to ConfigurationSnapshot<T> and HistoryManager<T>
- [ ] **T029** Add XML documentation for all new public API members
- [ ] **T030** Verify backward compatibility with existing HashRing usage

## Phase 3.5: Quality & Packaging

### Comprehensive Testing [P]
- [ ] **T031** [P] Edge case unit tests for empty rings with history in `test/Common.Utilities.UnitTests/ConsistentHashing/VersionAwareEdgeCaseTests.cs`
- [ ] **T032** [P] Stress tests for history management in `test/Common.Utilities.UnitTests/ConsistentHashing/VersionAwareStressTests.cs`

### Samples and Documentation [P]
- [ ] **T033** [P] Create version-aware HashRing sample in `samples/Common.Utilities.Samples/ConsistentHashing/VersionAwareMigrationExample.cs`
- [ ] **T034** [P] Update existing samples to demonstrate backward compatibility

### Quality Gates
- [ ] **T035** Verify all Roslynator analyzer rules pass
- [ ] **T036** Run existing ConsistentHashing tests to ensure no regressions
- [ ] **T037** Validate test coverage meets project standards
- [ ] **T038** Performance validation: version-aware operations maintain O(log n) complexity

## Dependencies

### Critical Paths
1. **Setup** (T001-T003) → **All other phases**
2. **Tests** (T004-T013) → **Implementation** (T014-T027) - NON-NEGOTIABLE TDD
3. **Internal Entities** (T014-T015) → **HashRing Extensions** (T018-T025)
4. **Core Implementation** → **Integration** (T028-T030) → **Quality** (T031-T038)

### Sequential Dependencies (Same File)
- HashRingOptions: T018 (extend options)
- HashRing.cs: T019 → T020 → T021 → T022 → T023 → T024 → T025 → T026 → T027 (must be sequential)

## Parallel Execution Examples

### TDD Phase (T004-T013)
```bash
# Launch contract tests together:
Task: "Version-aware HashRingOptions contract test in test/Common.Utilities.UnitTests/ConsistentHashing/HashRingOptionsVersionTests.cs"
Task: "ServerCandidateResult<T> contract test in test/Common.Utilities.UnitTests/ConsistentHashing/ServerCandidateResultTests.cs"
Task: "HashRingHistoryLimitExceededException contract test in test/Common.Utilities.UnitTests/ConsistentHashing/HashRingHistoryLimitExceededExceptionTests.cs"
Task: "ConfigurationSnapshot<T> unit test in test/Common.Utilities.UnitTests/ConsistentHashing/ConfigurationSnapshotTests.cs"
Task: "HistoryManager<T> unit test in test/Common.Utilities.UnitTests/ConsistentHashing/HistoryManagerTests.cs"
```

### Implementation Phase (T014-T017)
```bash
# Launch entity implementations together:
Task: "ConfigurationSnapshot<T> implementation in src/Common.Utilities/ConsistentHashing/ConfigurationSnapshot.cs"
Task: "HistoryManager<T> implementation in src/Common.Utilities/ConsistentHashing/HistoryManager.cs"
Task: "ServerCandidateResult<T> implementation in src/Common.Utilities/ConsistentHashing/ServerCandidateResult.cs"
Task: "HashRingHistoryLimitExceededException implementation in src/Common.Utilities/ConsistentHashing/HashRingHistoryLimitExceededException.cs"
```

### Quality Phase (T031-T034)
```bash
# Launch quality tasks together:
Task: "Edge case unit tests for empty rings with history in test/Common.Utilities.UnitTests/ConsistentHashing/VersionAwareEdgeCaseTests.cs"
Task: "Stress tests for history management in test/Common.Utilities.UnitTests/ConsistentHashing/VersionAwareStressTests.cs"
Task: "Create version-aware HashRing sample in samples/Common.Utilities.Samples/ConsistentHashing/VersionAwareMigrationExample.cs"
```

## Notes
- [P] tasks target different files and have no shared dependencies
- All HashRing.cs modifications (T018-T027) must be sequential due to same-file conflicts
- Each test task must create failing tests before corresponding implementation
- Commit after completing each task
- Integration tests should follow quickstart scenarios

## Task Generation Rules Applied

1. **From Contracts**: 3 contract files → 3 contract test tasks [P]
2. **From Data Model**: 3 entities → 3 model creation tasks [P]
3. **From User Stories**: 4 scenarios → 4 integration tests [P]
4. **From API Surface**: 8 new methods → 8 implementation tasks (sequential due to same file)

## Validation Checklist

- [x] All public API members have corresponding tests (T004-T006, T009)
- [x] All entities have implementation tasks (T014-T017)
- [x] All tests come before implementation (Phase 3.2 → Phase 3.3)
- [x] Parallel tasks use different .cs files ([P] markers validated)
- [x] Each task specifies exact file path with .cs extension
- [x] No [P] task modifies same file (HashRing.cs tasks are sequential)

## Constitution Compliance

- [x] TDD approach strictly enforced (Tests T004-T013 before Implementation T014-T027)
- [x] Library extends existing with clear boundaries (ConsistentHashing namespace)
- [x] Quality gates configured (Roslynator validation T035)
- [x] Single domain focus maintained (version-aware consistent hashing only)
- [x] API follows consistent .NET patterns (extends existing HashRing<T>)