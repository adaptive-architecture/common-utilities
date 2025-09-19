# Implementation Plan: Consistent Hash Ring Utility

**Branch**: `001-implement-a-consitent` | **Date**: 2025-09-19 | **Spec**: [link](./spec.md)
**Input**: Feature specification from `C:\Users\ValentinDide\code\github.com\adaptive-architecture\common-utilities\specs\001-implement-a-consitent\spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → ✅ Feature spec loaded successfully
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → ✅ All clarifications resolved in spec
   → ✅ Project Type detected as single .NET library project
3. Fill the Constitution Check section based on the content of the constitution document.
   → ✅ Constitution principles mapped to requirements
4. Evaluate Constitution Check section below
   → ✅ No violations found, all principles aligned
   → ✅ Progress Tracking: Initial Constitution Check PASS
5. Execute Phase 0 → research.md
   → ✅ Research completed for hash algorithms and virtual nodes
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, CLAUDE.md
   → ✅ Design artifacts generated
7. Re-evaluate Constitution Check section
   → ✅ No new violations, design aligns with principles
   → ✅ Progress Tracking: Post-Design Constitution Check PASS
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
   → ✅ Task generation strategy described
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 9. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Implement consistent hashing functionality within the existing Common.Utilities library for distributed system request routing. The feature will provide a HashRing class that maps request IDs (byte arrays) to servers using configurable hash algorithms (SHA1/MD5) and virtual nodes for balanced distribution. Key features include thread-safe operations, dynamic server addition/removal, and minimal redistribution during scaling events.

## Technical Context
**Language/Version**: C# with .NET 9.0 and preview language features enabled
**Primary Dependencies**: System.Collections.Concurrent (thread safety), System.Security.Cryptography (hashing algorithms) - already available in Common.Utilities
**Storage**: N/A - in-memory data structure
**Testing**: xUnit for unit tests, no external dependencies requiring TestContainers
**Target Platform**: .NET 9.0 compatible platforms (Windows, Linux, macOS)
**Project Type**: single - .NET library project following AdaptArch.Common.Utilities.* namespace convention
**Performance Goals**: O(log N) lookup time, minimal memory allocation during operations
**Constraints**: Thread-safe for concurrent access, configurable virtual nodes (default 42), support for 1000+ servers
**Scale/Scope**: Single library with ~5-10 public classes, comprehensive test coverage, sample project

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **Principle I - Library-First Design**: Clear module boundaries within Common.Utilities, no separate package needed due to minimal dependencies
✅ **Principle II - Test-Driven Development**: All requirements testable, TDD approach enforced
✅ **Principle III - Quality-First Standards**: Warnings as errors, Roslynator analyzers, .NET 9.0 target framework
✅ **Principle IV - Modular Specialization**: Single domain focus (consistent hashing), minimal dependencies
✅ **Principle V - API Consistency**: Standard .NET patterns, extension methods for common ID types, IEquatable support

**Assessment**: All constitutional principles align with feature requirements. No violations detected.

## Project Structure

### Documentation (this feature)
```
specs/001-implement-a-consitent/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Integration into existing Common.Utilities project
src/
└── Common.Utilities/
    ├── ConsistentHashing/
    │   ├── HashRing.cs
    │   ├── IHashAlgorithm.cs
    │   ├── VirtualNode.cs
    │   └── Extensions/
    │       └── HashRingExtensions.cs
    └── Common.Utilities.csproj (existing)

test/
└── Common.Utilities.Tests/
    ├── ConsistentHashing/
    │   ├── HashRingTests.cs
    │   ├── VirtualNodeTests.cs
    │   └── ExtensionsTests.cs
    └── Common.Utilities.Tests.csproj (existing)

samples/
└── Common.Utilities.Samples/
    └── ConsistentHashing/
        ├── DatabaseRoutingExample.cs
        └── HttpRoutingExample.cs
```

**Structure Decision**: Integration into existing Common.Utilities project - no external dependencies justify separate package

## Phase 0: Outline & Research

**Research Areas Completed**:

### Hash Algorithm Selection
- **Decision**: Configurable IHashAlgorithm interface with SHA1 and MD5 implementations
- **Rationale**: SHA1 provides good distribution with acceptable performance; MD5 faster but less secure; configurability allows future algorithms
- **Alternatives considered**: Fixed SHA256 (too slow), xxHash (external dependency violates minimal dependency principle)

### Virtual Node Configuration
- **Decision**: Configurable virtual nodes per server, default 42
- **Rationale**: 42 provides good distribution balance based on consistent hashing research; configurable to allow tuning for specific cluster sizes and distribution requirements

### Thread Safety Implementation
- **Decision**: ConcurrentSortedDictionary for virtual node storage with reader-writer locks for server management
- **Rationale**: Allows concurrent reads while protecting writes; minimal contention for common lookup operations
- **Alternatives considered**: Full immutable snapshots (memory overhead), simple locks (contention issues)

**Output**: research.md with all technical decisions resolved

## Phase 1: Design & Contracts

### Data Model Entities
1. **HashRing<T>**: Generic container managing server-to-virtual-node mappings
2. **VirtualNode**: Represents hash position and associated server
3. **IHashAlgorithm**: Interface for pluggable hash algorithms
4. **Server Identification**: Generic type T with IEquatable<T> constraint

### API Contracts
- `HashRing<T>.Add(T server, int virtualNodes = 42)`: Add server with virtual nodes
- `HashRing<T>.Remove(T server)`: Remove server and its virtual nodes
- `HashRing<T>.GetServer(byte[] key)`: Find server for given key
- `HashRing<T>.GetServer(string key)`: Extension method for string keys
- `HashRing<T>.GetServer(Guid key)`: Extension method for Guid keys
- `HashRing<T>.Servers`: Enumerate all configured servers

### Integration Test Scenarios
1. Consistency verification across multiple calls
2. Minimal redistribution on server addition
3. Failover behavior on server removal
4. Thread safety under concurrent access

**Output**: data-model.md, /contracts/*, quickstart.md, CLAUDE.md updated

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (data model, API contracts)
- Each public class → unit test task [P]
- Each API method → contract test task [P]
- Integration scenarios → integration test tasks
- Implementation tasks following TDD order

**Ordering Strategy**:
- TDD order: Tests before implementation (NON-NEGOTIABLE per Constitution Principle II)
- Dependency order: Core types → algorithms → extensions → integration
- Mark [P] for parallel execution (different .cs files)

**Estimated Output**: 20-25 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)
**Phase 4**: Implementation (execute tasks.md following constitutional principles)
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*No constitutional violations detected - this section is empty*

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented (none)

---
*Based on Constitution v1.0.0 - See `.specify/memory/constitution.md`*
