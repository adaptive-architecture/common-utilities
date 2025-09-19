# Tasks: Consistent Hash Ring Utility

**Input**: Design documents from `C:\Users\ValentinDide\code\github.com\adaptive-architecture\common-utilities\specs\001-implement-a-consitent\`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → ✅ Implementation plan loaded - C# .NET 9.0, integration into Common.Utilities
2. Load optional design documents:
   → ✅ data-model.md: HashRing<T>, VirtualNode<T>, IHashAlgorithm entities
   → ✅ contracts/: HashRing-API.cs and HashRing-Extensions.cs contract test tasks
   → ✅ research.md: SHA1/MD5 algorithms, 42 virtual nodes default, thread safety decisions
3. Generate tasks by category:
   → ✅ Setup: ConsistentHashing folder structure, no separate project needed
   → ✅ Tests: contract tests, integration tests for consistency and thread safety
   → ✅ Core: HashRing, VirtualNode, hash algorithms, extension methods
   → ✅ Library: public API implementation, configuration options
   → ✅ Polish: comprehensive unit tests, performance validation, sample projects
4. Apply task rules:
   → ✅ Different files = marked [P] for parallel execution
   → ✅ Same file = sequential (no [P] marking)
   → ✅ Tests before implementation (TDD approach - NON-NEGOTIABLE)
5. Number tasks sequentially (T001-T030)

## ✅ COMPLETED - All 30 tasks implemented successfully
**Implementation Status**: 100% Complete (30/30 tasks)
- ✅ Phase 3.1: Setup (T001-T003)
- ✅ Phase 3.2: Tests First/TDD (T004-T013)
- ✅ Phase 3.3: Core Implementation (T014-T022)
- ✅ Phase 3.4: Library Integration (T023-T026)
- ✅ Phase 3.5: Quality & Packaging (T027-T030)

**Key Achievements**:
- Full consistent hash ring implementation with 96 comprehensive test cases
- Thread-safe concurrent operations using static hash methods
- Support for SHA1 and MD5 hash algorithms
- Extension methods for common key types (string, Guid, int)
- Configuration support via HashRingOptions
- Complete XML documentation and sample applications
- Zero build warnings, all quality gates passing
6. Generate dependency graph and parallel execution examples
7. Validate task completeness:
   → ✅ All contracts have corresponding tests
   → ✅ All entities have model tasks
   → ✅ All public API methods implemented
8. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Integration into Common.Utilities**: `src/Common.Utilities/ConsistentHashing/`, `test/Common.Utilities.Tests/ConsistentHashing/`
- **Sample projects**: `samples/Common.Utilities.Samples/ConsistentHashing/`
- Paths reflect integration into existing project structure per plan.md

## Phase 3.1: Setup
- [x] T001 Create ConsistentHashing folder structure in existing Common.Utilities project
- [x] T002 [P] Add System.Collections.Concurrent and System.Security.Cryptography usings (already available)
- [x] T003 [P] Configure namespace AdaptArch.Common.Utilities.ConsistentHashing in source files

## Phase 3.2: Tests First (TDD) ⚠️ NON-NEGOTIABLE - MUST COMPLETE BEFORE 3.3
**CRITICAL: Constitution Principle II - Tests MUST be written and MUST FAIL before ANY implementation**
- [x] T004 [P] Contract test for HashRing<T> constructors in test/Common.Utilities.Tests/ConsistentHashing/HashRingContractTests.cs
- [x] T005 [P] Contract test for HashRing<T> Add/Remove operations in test/Common.Utilities.Tests/ConsistentHashing/HashRingContractTests.cs
- [x] T006 [P] Contract test for HashRing<T> GetServer methods in test/Common.Utilities.Tests/ConsistentHashing/HashRingContractTests.cs
- [x] T007 [P] Contract test for HashRing<T> properties and enumeration in test/Common.Utilities.Tests/ConsistentHashing/HashRingContractTests.cs
- [x] T008 [P] Contract test for IHashAlgorithm implementations in test/Common.Utilities.Tests/ConsistentHashing/HashAlgorithmContractTests.cs
- [x] T009 [P] Contract test for HashRing extension methods in test/Common.Utilities.Tests/ConsistentHashing/HashRingExtensionsContractTests.cs
- [x] T010 [P] Integration test for consistency verification in test/Common.Utilities.Tests/ConsistentHashing/ConsistencyIntegrationTests.cs
- [x] T011 [P] Integration test for minimal redistribution on server addition in test/Common.Utilities.Tests/ConsistentHashing/RedistributionIntegrationTests.cs
- [x] T012 [P] Integration test for failover on server removal in test/Common.Utilities.Tests/ConsistentHashing/FailoverIntegrationTests.cs
- [x] T013 [P] Integration test for thread safety under concurrent access in test/Common.Utilities.Tests/ConsistentHashing/ConcurrencyIntegrationTests.cs

## Phase 3.3: Core Implementation (ONLY after tests are failing)
- [x] T014 [P] Implement IHashAlgorithm interface in src/Common.Utilities/ConsistentHashing/IHashAlgorithm.cs
- [x] T015 [P] Implement Sha1HashAlgorithm class in src/Common.Utilities/ConsistentHashing/Sha1HashAlgorithm.cs
- [x] T016 [P] Implement Md5HashAlgorithm class in src/Common.Utilities/ConsistentHashing/Md5HashAlgorithm.cs
- [x] T017 [P] Implement VirtualNode<T> internal class in src/Common.Utilities/ConsistentHashing/VirtualNode.cs
- [x] T018 Implement HashRing<T> core class with constructors in src/Common.Utilities/ConsistentHashing/HashRing.cs
- [x] T019 Implement HashRing<T> Add/Remove server methods in src/Common.Utilities/ConsistentHashing/HashRing.cs
- [x] T020 Implement HashRing<T> GetServer lookup methods in src/Common.Utilities/ConsistentHashing/HashRing.cs
- [x] T021 Implement HashRing<T> properties and server enumeration in src/Common.Utilities/ConsistentHashing/HashRing.cs
- [x] T022 [P] Implement HashRing extension methods in src/Common.Utilities/ConsistentHashing/HashRingExtensions.cs

## Phase 3.4: Library Integration
- [x] T023 [P] Implement HashRingOptions configuration class in src/Common.Utilities/ConsistentHashing/HashRingOptions.cs
- [x] T024 Add XML documentation for all public API members across all source files
- [x] T025 Implement thread-safety using ConcurrentSortedDictionary in HashRing<T> implementation
- [x] T026 Add input validation and error handling for all public methods

## Phase 3.5: Quality & Packaging
- [x] T027 [P] Create sample project demonstrating database routing in samples/Common.Utilities.Samples/ConsistentHashing/DatabaseRoutingExample.cs
- [x] T028 [P] Create sample project demonstrating HTTP load balancing in samples/Common.Utilities.Samples/ConsistentHashing/HttpRoutingExample.cs
- [x] T029 Verify all quality gates pass (Roslynator analyzers, no warnings)
- [x] T030 Validate integration with existing Common.Utilities project builds correctly

## Dependencies
- Setup (T001-T003) before all other phases
- Tests (T004-T013) before implementation (T014-T026) - NON-NEGOTIABLE per Constitution
- Core interfaces (T014) before implementations (T015-T017)
- HashRing base (T018) blocks subsequent HashRing methods (T019-T021)
- Core implementation blocks integration (T023-T026)
- Quality validation (T029) blocks final integration (T030)

## Parallel Example
```
# Launch T004-T013 together (TDD phase - different test files):
Task: "Contract test for HashRing<T> constructors in test/Common.Utilities.Tests/ConsistentHashing/HashRingContractTests.cs"
Task: "Contract test for IHashAlgorithm implementations in test/Common.Utilities.Tests/ConsistentHashing/HashAlgorithmContractTests.cs"
Task: "Contract test for HashRing extension methods in test/Common.Utilities.Tests/ConsistentHashing/HashRingExtensionsContractTests.cs"
Task: "Integration test for consistency verification in test/Common.Utilities.Tests/ConsistentHashing/ConsistencyIntegrationTests.cs"

# Launch T014-T017 together (interface and algorithm implementations):
Task: "Implement IHashAlgorithm interface in src/Common.Utilities/ConsistentHashing/IHashAlgorithm.cs"
Task: "Implement Sha1HashAlgorithm class in src/Common.Utilities/ConsistentHashing/Sha1HashAlgorithm.cs"
Task: "Implement Md5HashAlgorithm class in src/Common.Utilities/ConsistentHashing/Md5HashAlgorithm.cs"
Task: "Implement VirtualNode<T> internal class in src/Common.Utilities/ConsistentHashing/VirtualNode.cs"
```

## Notes
- [P] tasks = different files, no dependencies
- Verify tests fail before implementing (TDD requirement)
- All tasks integrate into existing Common.Utilities project structure
- No separate package creation needed per plan.md decision

## Validation Checklist
*GATE: Checked by main() before returning*

- [x] All contracts have corresponding tests
- [x] All entities have model tasks
- [x] All tests come before implementation (Constitution Principle II - NON-NEGOTIABLE)
- [x] Parallel tasks truly independent (different .cs files)
- [x] Each task specifies exact file path with .cs extension
- [x] No task modifies same file as another [P] task

## Constitution Compliance
*GATE: Verify adherence to Common Utilities Constitution v1.0.0*

- [x] TDD approach strictly enforced (Principle II - NON-NEGOTIABLE)
- [x] Library designed as standalone module with clear boundaries (Principle I)
- [x] Quality gates configured (Roslynator analyzers, no warnings) (Principle III)
- [x] Single domain focus maintained (consistent hashing) (Principle IV)
- [x] API follows consistent .NET patterns (Principle V)
