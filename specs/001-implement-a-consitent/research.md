# Research: Consistent Hash Ring Implementation

## Hash Algorithm Analysis

### Decision: Configurable Hash Algorithm Interface
**Chosen Approach**: IHashAlgorithm interface with SHA1 and MD5 implementations
**Rationale**:
- SHA1 provides excellent distribution characteristics with reasonable performance
- MD5 offers faster performance for scenarios where cryptographic security is not critical
- Interface allows future extension without breaking changes
- Maintains constitutional principle of minimal dependencies

**Alternatives Considered**:
- **SHA256**: Excellent security but ~30% slower than SHA1, overkill for load balancing
- **xxHash**: Superior performance but requires external dependency, violates minimal dependency principle
- **Fixed Algorithm**: Would limit flexibility for different use cases

### Virtual Node Configuration Analysis

### Decision: Configurable Virtual Nodes with 42 Default
**Chosen Approach**: Constructor parameter with sensible default
**Rationale**:
- 42 virtual nodes provides good distribution balance based on consistent hashing research
- Configurable to allow tuning for specific cluster sizes and distribution requirements
- Memory overhead remains reasonable (~0.4KB per server for 42 nodes)
- Higher values improve distribution uniformity but increase memory usage

**Alternatives Considered**:
- **Fixed 150**: Common in some implementations but insufficient for large clusters (>100 servers)
- **Fixed 1000+**: Better distribution but excessive memory overhead for small clusters
- **Automatic Scaling**: Too complex for utility library, violates simplicity principle

## Thread Safety Strategy

### Decision: ConcurrentSortedDictionary with Read-Write Locks
**Chosen Approach**: Hybrid approach using .NET concurrent collections
**Rationale**:
- Frequent read operations (GetServer) can proceed concurrently
- Infrequent write operations (Add/Remove) use coordination for consistency
- Leverages .NET's optimized concurrent collections
- Minimal contention for common use case (many reads, few writes)

**Alternatives Considered**:
- **Immutable Snapshots**: Eliminates locks but creates memory pressure on writes
- **Single ReaderWriterLockSlim**: Simple but creates contention bottleneck
- **Lock-Free Algorithms**: Complex implementation, risk of bugs, overkill for utility

## Performance Characteristics

### Lookup Performance: O(log N)
- Binary search on sorted virtual node array
- Consistent performance regardless of hash ring size
- Memory-efficient storage using sorted data structures

### Memory Usage Analysis
- Base overhead: ~200 bytes per HashRing instance
- Per-server overhead: ~8 bytes × virtual node count (default 3.3KB)
- Virtual node storage: 16 bytes per node (hash + server reference)
- Total for 100 servers: ~330KB (acceptable for most applications)

## .NET Integration Patterns

### Generic Type Constraints
**Decision**: `HashRing<T> where T : IEquatable<T>`
**Rationale**: Ensures proper equality semantics for server identification while maintaining type safety

### Extension Methods
**Decision**: Provide convenience extensions for common key types
```csharp
public static T GetServer<T>(this HashRing<T> ring, string key)
public static T GetServer<T>(this HashRing<T> ring, Guid key)
```
**Rationale**: Follows .NET convention of extension methods for common scenarios

### Configuration Integration
**Decision**: Support IOptions<T> pattern for DI scenarios
**Rationale**: Aligns with standard .NET configuration patterns, enables easy testing and deployment configuration

## Implementation Strategy

### Core Algorithm
1. **Initialization**: Create sorted virtual node array for each server
2. **Lookup**: Binary search for key hash position, return next server clockwise
3. **Add Server**: Insert virtual nodes and re-sort (write-locked operation)
4. **Remove Server**: Remove virtual nodes and compact array (write-locked operation)

### Error Handling Strategy
- Empty ring: Return default(T) or throw configurable exception
- Null/empty keys: Throw ArgumentException with clear message
- Duplicate servers: Replace existing server's virtual nodes
- Concurrent modification: Use standard .NET concurrent collection patterns

## Testing Strategy

### Unit Test Categories
1. **Algorithm Correctness**: Verify consistent mapping and distribution
2. **Edge Cases**: Empty rings, null inputs, duplicate servers
3. **Performance**: Benchmark lookup times and memory usage
4. **Thread Safety**: Concurrent access verification

### Integration Test Scenarios
1. **Multi-threaded Stress Testing**: Concurrent reads/writes
2. **Distribution Analysis**: Statistical verification of balanced distribution
3. **Real-world Simulation**: HTTP routing and database connection scenarios
