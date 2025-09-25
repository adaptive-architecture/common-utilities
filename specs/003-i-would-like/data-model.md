# Data Model: Version-Aware ConsistentHashing.HashRing

**Date**: 2025-09-25
**Feature**: Version-Aware ConsistentHashing.HashRing Extension

## Core Entities

### ConfigurationSnapshot<T>
Represents an immutable snapshot of a hash ring configuration at a specific point in time.

**Fields**:
- `Id`: `Guid` - Unique identifier for the snapshot
- `Timestamp`: `DateTimeOffset` - When the snapshot was created
- `Servers`: `IReadOnlyDictionary<T, int>` - Server to virtual node count mapping
- `VirtualNodes`: `IReadOnlyList<VirtualNode<T>>` - Sorted virtual nodes for this configuration
- `HashAlgorithm`: `IHashAlgorithm` - Hash algorithm used (reference, not ownership)

**Validation Rules**:
- Id must not be empty
- Servers collection must not be null or empty
- VirtualNodes must be sorted by hash value
- All virtual nodes must reference servers in the Servers collection
- HashAlgorithm must not be null

**State Transitions**:
- Immutable once created
- No state transitions (read-only entity)

### HistoryManager<T>
Manages the circular buffer of configuration snapshots with limit enforcement.

**Fields**:
- `MaxHistorySize`: `int` - Maximum number of snapshots to retain
- `History`: `CircularBuffer<ConfigurationSnapshot<T>>` - Internal circular buffer
- `Count`: `int` - Current number of snapshots in history

**Validation Rules**:
- MaxHistorySize must be >= 1
- History must not exceed MaxHistorySize
- All snapshots in history must be valid ConfigurationSnapshot instances

**State Transitions**:
1. **Empty** → **HasHistory**: When first snapshot is added
2. **HasHistory** → **Full**: When history reaches MaxHistorySize
3. **Full** → **Full**: When new snapshot would exceed limit (exception thrown)

### ServerCandidateResult<T>
Represents the result of querying for servers across configuration versions.

**Fields**:
- `Servers`: `IReadOnlyList<T>` - Unique servers in priority order
- `ConfigurationCount`: `int` - Number of configurations consulted
- `HasHistory`: `bool` - Whether historical configurations were available

**Validation Rules**:
- Servers list must not contain duplicates
- Servers must be in priority order (current first, then historical)
- ConfigurationCount must be >= 1 if any servers returned

## Extended HashRing<T> Entity

### New Fields
- `_configurationHistory`: `HistoryManager<T>?` - Optional history manager (null if history disabled)
- `_historyEnabled`: `bool` - Whether version-aware features are enabled
- `_maxHistorySize`: `int` - Maximum number of historical configurations to retain

### New Validation Rules
- MaxHistorySize must be >= 1 when history is enabled
- Configuration snapshots must be created only when explicitly requested
- Historical configurations must be immutable after creation

### New State Transitions
1. **Standard HashRing** → **Version-Aware HashRing**: When history is enabled via options
2. **Version-Aware HashRing** → **Version-Aware HashRing with History**: When first snapshot is created
3. **History Management**: Creation of new snapshots, clearing history

## Configuration Extension

### HashRingOptions Updates
**New Fields**:
- `EnableVersionHistory`: `bool` - Whether to enable version-aware features (default: false)
- `MaxHistorySize`: `int` - Maximum history size when enabled (default: 3)

**Validation Rules**:
- MaxHistorySize must be >= 1 when EnableVersionHistory is true
- MaxHistorySize is ignored when EnableVersionHistory is false

## Entity Relationships

```
HashRing<T>
├── HistoryManager<T> (0..1)
│   └── ConfigurationSnapshot<T> (0..MaxHistorySize)
│       ├── VirtualNode<T> (*)
│       └── IHashAlgorithm (1)
└── ServerCandidateResult<T> (returned by queries)
```

## Data Flow

### Snapshot Creation Flow
1. Current HashRing state → ConfigurationSnapshot creation
2. Validation of snapshot integrity
3. HistoryManager.AddSnapshot() → Circular buffer management
4. Exception if history limit would be exceeded

### Server Lookup Flow
1. Key input → Hash computation
2. Current configuration lookup → Primary server
3. Historical configuration lookups → Secondary servers
4. Deduplication → ServerCandidateResult with unique servers

### History Management Flow
1. Snapshot creation → Add to circular buffer
2. History clearing → Reset buffer, retain current state
3. Limit enforcement → Exception on overflow attempt

## Memory Considerations

### Storage Complexity
- **Per Snapshot**: O(S + V) where S = servers, V = virtual nodes
- **Total History**: O(H × (S + V)) where H = history size
- **Typical Usage**: 3 snapshots × (10 servers × 42 virtual nodes) ≈ 1.3KB per ring

### Performance Impact
- Snapshot creation: O(V) for virtual node copying
- Server lookup: O(H × log V) for historical lookups
- Memory overhead: Linear with history size and ring complexity