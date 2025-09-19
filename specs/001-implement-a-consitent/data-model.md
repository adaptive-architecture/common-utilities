# Data Model: Consistent Hash Ring

## Core Entities

### HashRing<T>
**Purpose**: Main container managing the consistent hash ring and server distribution
**Generic Constraint**: `T : IEquatable<T>` for proper server identification
**Thread Safety**: Thread-safe for concurrent reads, coordinated writes

**Key Properties**:
- `IReadOnlyCollection<T> Servers`: Enumeration of all configured servers
- `int VirtualNodeCount`: Default virtual nodes per server (configurable)
- `IHashAlgorithm HashAlgorithm`: Configurable hashing implementation

**Core Operations**:
- `Add(T server, int? virtualNodes = null)`: Add server with optional virtual node override
- `Remove(T server)`: Remove server and all its virtual nodes
- `GetServer(byte[] key)`: Primary lookup method returning server for key
- `Clear()`: Remove all servers from ring
- `Contains(T server)`: Check if server is in ring

### VirtualNode<T>
**Purpose**: Represents a hash position on the ring associated with a physical server
**Internal Structure**: Used internally by HashRing, not exposed in public API

**Properties**:
- `uint Hash`: Hash value position on ring (0 to uint.MaxValue)
- `T Server`: Reference to physical server this virtual node represents
- `int NodeIndex`: Index within server's virtual nodes (for debugging/monitoring)

**Relationships**:
- Many VirtualNodes → One Server (1:N relationship)
- VirtualNodes are sorted by Hash value in ring structure

### IHashAlgorithm
**Purpose**: Abstraction for pluggable hash algorithms
**Implementation Strategy**: Factory pattern with built-in implementations

**Interface**:
```csharp
public interface IHashAlgorithm
{
    uint ComputeHash(byte[] data);
    string Name { get; }
}
```

**Built-in Implementations**:
- `Sha1HashAlgorithm`: Default implementation using SHA1
- `Md5HashAlgorithm`: Faster alternative using MD5
- Future: Extensible for custom algorithms

### HashRingConfiguration
**Purpose**: Configuration options for dependency injection scenarios
**Pattern**: Standard .NET IOptions<T> configuration

**Properties**:
- `int DefaultVirtualNodes`: Default virtual node count (42)
- `IHashAlgorithm HashAlgorithm`: Algorithm instance
- `bool ThrowOnEmptyRing`: Exception behavior for empty ring lookups

## Internal Data Structures

### Virtual Node Storage
**Implementation**: `ConcurrentSortedList<uint, T>` or similar structure
**Key Design Decisions**:
- Sorted by hash value for O(log N) binary search
- Concurrent collection for thread-safe read operations
- Write operations use coordination mechanism

### Server Metadata
**Storage**: `ConcurrentDictionary<T, ServerMetadata>`
**Purpose**: Track server-specific information
```csharp
internal class ServerMetadata
{
    public int VirtualNodeCount { get; set; }
    public uint[] NodeHashes { get; set; }
    public DateTime AddedAt { get; set; }
}
```

## State Transitions

### Adding Servers
1. **Validate**: Check server not null, not already exists (or replace)
2. **Generate Virtual Nodes**: Create N hash positions for server
3. **Update Ring**: Insert virtual nodes in sorted order (write-locked)
4. **Update Metadata**: Store server information for future removal

### Removing Servers
1. **Validate**: Check server exists in ring
2. **Locate Virtual Nodes**: Find all hash positions for server
3. **Update Ring**: Remove virtual nodes from sorted structure (write-locked)
4. **Cleanup Metadata**: Remove server tracking information

### Lookup Operations
1. **Hash Key**: Compute hash of input byte array
2. **Binary Search**: Find next hash position >= computed hash
3. **Handle Wraparound**: If no position found, return first server (ring property)
4. **Return Server**: Extract server from virtual node at found position

## Validation Rules

### Input Validation
- **Server**: Must not be null, must implement IEquatable<T>
- **Keys**: Byte arrays must not be null (empty arrays allowed)
- **Virtual Node Count**: Must be positive integer (1-10000 reasonable range)

### Business Rules
- **Unique Servers**: Each server can only be added once (replacement allowed)
- **Minimum Servers**: Ring must have at least 1 server for lookups
- **Hash Distribution**: Virtual nodes distributed across full uint range

### Error Conditions
- **Empty Ring Lookup**: Configurable behavior (exception or default value)
- **Concurrent Modification**: Protected by thread synchronization
- **Hash Collisions**: Handled by deterministic ordering (stable sort)

## Performance Characteristics

### Time Complexity
- **Lookup**: O(log N) where N = total virtual nodes
- **Add Server**: O(V log N) where V = virtual nodes for server
- **Remove Server**: O(V log N) for virtual node removal
- **Enumeration**: O(S) where S = number of physical servers

### Memory Usage
- **Base Overhead**: ~200 bytes per HashRing instance
- **Per Virtual Node**: ~20 bytes (hash + reference + metadata)
- **Per Server**: ~50 bytes + (virtual node count × 20 bytes)
- **Total Example**: 100 servers × 42 nodes = ~84KB (default), 1000 servers × 1000 nodes = ~2MB (high distribution)

### Scalability Limits
- **Practical Server Limit**: 10,000 servers (manageable memory usage)
- **Virtual Node Limit**: 50,000 per server (memory constraint)
- **Concurrent Read Limit**: Bounded by system threading capacity
- **Write Contention**: Minimal due to infrequent add/remove operations
