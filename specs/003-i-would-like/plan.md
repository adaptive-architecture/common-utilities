
# Implementation Plan: Version-Aware ConsistentHashing.HashRing

**Branch**: `003-i-would-like` | **Date**: 2025-09-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `C:\Users\ValentinDide\code\github.com\adaptive-architecture\common-utilities\specs\003-i-would-like\spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Extend the existing ConsistentHashing.HashRing to support version-aware operations during data migration. The feature maintains a configurable history of previous server configurations, enabling gradual data migration by returning server candidates from both current and historical configurations in priority order.

## Technical Context
**Language/Version**: C# .NET 9.0 with preview language features
**Primary Dependencies**: Existing ConsistentHashing module (IHashAlgorithm, VirtualNode, HashRingOptions)
**Storage**: In-memory configuration history with configurable limits (no external storage)
**Testing**: xUnit with existing test patterns, TestContainers for integration tests
**Target Platform**: .NET 9.0 cross-platform (library target)
**Project Type**: Single utility library extension
**Performance Goals**: Maintain existing HashRing performance characteristics for lookup operations
**Constraints**: Thread-safety matching existing HashRing, backward compatibility required
**Scale/Scope**: Extension to existing ConsistentHashing namespace, minimal API surface expansion

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Library-First Design ✅ PASS
- Extends existing AdaptArch.Common.Utilities.ConsistentHashing library
- Maintains clear boundaries within ConsistentHashing namespace
- Self-contained version-aware functionality
- Well-defined public API extensions

### II. Test-Driven Development ✅ PASS
- Will follow Red-Green-Refactor cycle
- Unit tests for all new version-aware methods
- Integration tests using existing xUnit patterns
- Sample projects to demonstrate usage

### III. Quality-First Standards ✅ PASS
- Targets .NET 9.0 with preview features
- Will maintain existing Roslynator analyzer compliance
- No new warnings introduced
- Consistent with existing Directory.Build.props

### IV. Modular Specialization ✅ PASS
- Focused on consistent hashing version-awareness only
- No dependencies on other utility libraries
- Extends existing domain (ConsistentHashing)
- Minimal additional complexity

### V. API Consistency ✅ PASS
- Follows existing HashRing patterns
- Consistent error handling with current implementation
- Maintains backward compatibility
- Public API surface follows established conventions

**Initial Constitution Check: PASS** - No violations detected

### Post-Design Constitution Re-evaluation

#### I. Library-First Design ✅ PASS
- API contracts maintain clean separation in ConsistentHashing namespace
- Data model follows existing entity patterns
- No cross-library dependencies introduced

#### II. Test-Driven Development ✅ PASS
- Contract files define testable API surface
- Quickstart provides clear test scenarios
- Integration with existing xUnit test patterns confirmed

#### III. Quality-First Standards ✅ PASS
- Contracts follow existing C# conventions
- No new analyzer warnings expected
- Consistent with project quality standards

#### IV. Modular Specialization ✅ PASS
- Focused extension of ConsistentHashing domain only
- No feature creep or unrelated functionality
- Clean separation of version-aware features

#### V. API Consistency ✅ PASS
- Follows established HashRing method patterns
- Exception handling consistent with existing implementation
- Configuration extension follows HashRingOptions pattern

**Post-Design Constitution Check: PASS** - Design maintains constitutional compliance

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure]
```

**Structure Decision**: [DEFAULT to Option 1 unless Technical Context indicates web/mobile app]

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task
- Implementation tasks to make tests pass

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


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
- [x] Complexity deviations documented (None required)

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
