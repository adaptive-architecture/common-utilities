# Consistent Hashing

The Consistent Hashing utility provides a robust implementation of the consistent hashing algorithm, designed for distributed systems that need to distribute keys across multiple servers while minimizing redistribution when servers are added or removed.

## Overview

Consistent hashing is a special kind of hashing algorithm that maps both keys and servers to positions on a circular ring. When a server is added or removed, only a small portion of keys need to be redistributed, making it ideal for:

- **Load Balancing**: Distribute HTTP requests across backend servers
- **Database Sharding**: Route database operations to specific shards
- **Caching**: Distribute cache keys across multiple cache instances
- **Service Discovery**: Route requests to service instances

## Key Features

- **Thread-Safe**: All operations are safe for concurrent access
- **Minimal Redistribution**: Adding/removing servers affects only adjacent keys
- **Configurable Virtual Nodes**: Control load distribution granularity
- **Multiple Hash Algorithms**: Support for SHA1 and MD5
- **Extension Methods**: Convenient methods for common key types
- **Type-Safe**: Generic implementation with strong typing

## Quick Start

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.ConsistentHashing;

// Create a hash ring
var ring = new HashRing<string>();

// Add servers
ring.Add("server1.example.com");
ring.Add("server2.example.com");
ring.Add("server3.example.com");

// Route a request
string server = ring.GetServer("user-12345");
Console.WriteLine($"Route user-12345 to: {server}");

// The same user always goes to the same server
string server2 = ring.GetServer("user-12345");
Console.WriteLine($"Consistent routing: {server == server2}"); // True
```

### With Custom Configuration

```csharp
// Create with custom options
var options = new HashRingOptions
{
    HashAlgorithm = new Md5HashAlgorithm(),
    DefaultVirtualNodes = 200
};

var ring = new HashRing<string>(options);

// Or create with custom hash algorithm directly
var ring2 = new HashRing<string>(new Sha1HashAlgorithm());
```

### Using Extension Methods

The library provides convenient extension methods for common key types:

```csharp
var ring = new HashRing<string>();
ring.Add("server1");
ring.Add("server2");

// Route by string key
string server1 = ring.GetServer("user-12345");

// Route by GUID
var userId = Guid.NewGuid();
string server2 = ring.GetServer(userId);

// Route by integer
string server3 = ring.GetServer(12345);
```

## Core Components

### HashRing&lt;T&gt; Class

The main class that implements the consistent hashing algorithm.

#### Constructors

```csharp
// Default constructor (uses SHA1)
var ring = new HashRing<string>();

// With custom hash algorithm
var ring = new HashRing<string>(new Md5HashAlgorithm());

// With configuration options
var ring = new HashRing<string>(new HashRingOptions
{
    DefaultVirtualNodes = 100
});
```

#### Key Methods

| Method | Description |
|--------|-------------|
| `Add(T server, int virtualNodes = 42)` | Adds a server with specified virtual nodes |
| `Remove(T server)` | Removes a server from the ring |
| `GetServer(byte[] key)` | Gets the server for a specific key |
| `TryGetServer(byte[] key, out T server)` | Safe version that doesn't throw |
| `GetServers(byte[] key, int count)` | Gets multiple servers in preference order |
| `Contains(T server)` | Checks if a server exists in the ring |
| `Clear()` | Removes all servers |

#### Properties

| Property | Description |
|----------|-------------|
| `Servers` | Read-only collection of all servers |
| `IsEmpty` | Whether the ring has no servers |
| `VirtualNodeCount` | Total number of virtual nodes |

### Hash Algorithms

#### IHashAlgorithm Interface

```csharp
public interface IHashAlgorithm
{
    byte[] ComputeHash(byte[] key);
}
```

#### Built-in Implementations

- **Sha1HashAlgorithm**: Uses SHA-1 hashing (default)
- **Md5HashAlgorithm**: Uses MD5 hashing (faster, less secure)

Both implementations are thread-safe and use static methods internally.

### Extension Methods

The `HashRingExtensions` class provides convenient overloads:

```csharp
// String keys
string server = ring.GetServer("user-id");
bool found = ring.TryGetServer("user-id", out string server);

// GUID keys
string server = ring.GetServer(Guid.NewGuid());
bool found = ring.TryGetServer(Guid.NewGuid(), out string server);

// Integer keys
string server = ring.GetServer(12345);
bool found = ring.TryGetServer(12345, out string server);
```

## Advanced Usage

### Database Connection Routing

```csharp
public class DatabaseRouter
{
    private readonly HashRing<DatabaseConnection> _ring;

    public DatabaseRouter()
    {
        _ring = new HashRing<DatabaseConnection>();

        // Add database connections with different capacities
        _ring.Add(new DatabaseConnection("primary", "Server=primary;..."), 300);
        _ring.Add(new DatabaseConnection("replica1", "Server=replica1;..."), 200);
        _ring.Add(new DatabaseConnection("replica2", "Server=replica2;..."), 200);
    }

    public DatabaseConnection GetDatabaseForUser(string userId)
    {
        return _ring.GetServer(userId);
    }
}

public record DatabaseConnection(string Name, string ConnectionString)
    : IEquatable<DatabaseConnection>;
```

### HTTP Load Balancing

```csharp
public class LoadBalancer
{
    private readonly HashRing<string> _ring;

    public LoadBalancer()
    {
        _ring = new HashRing<string>();
    }

    public void AddServer(string hostname, int capacity = 42)
    {
        _ring.Add(hostname, capacity);
    }

    public string RouteRequest(string sessionId)
    {
        return _ring.GetServer(sessionId);
    }

    public void HandleServerFailure(string hostname)
    {
        _ring.Remove(hostname);
        // Keys previously routed to this server will be
        // automatically redistributed to remaining servers
    }
}
```

### Handling Multiple Replicas

```csharp
// Get multiple servers for redundancy
var servers = ring.GetServers("important-key", 3).ToList();

// Try primary, fallback to replicas
foreach (var server in servers)
{
    try
    {
        // Attempt operation on server
        var result = await CallServer(server);
        return result;
    }
    catch (ServerUnavailableException)
    {
        // Try next server
        continue;
    }
}
```

## Virtual Nodes

Virtual nodes are multiple hash positions for each physical server on the ring. They provide:

### Benefits

- **Better Distribution**: More uniform load distribution
- **Reduced Hotspots**: Less chance of key clustering
- **Smoother Scaling**: Gradual redistribution when servers change

### Configuration

```csharp
// Default: 42 virtual nodes per server
ring.Add("server1");

// Custom virtual node count
ring.Add("high-capacity-server", 1000);  // More load
ring.Add("low-capacity-server", 100);    // Less load

// Configure default in options
var options = new HashRingOptions { DefaultVirtualNodes = 200 };
var ring = new HashRing<string>(options);
```

### Choosing Virtual Node Count

| Count Range | Use Case | Trade-offs |
|-------------|----------|------------|
| 50-100 | Small clusters, uniform servers | Fast, less memory |
| 200-500 | Medium clusters, mixed capacity | Balanced |
| 500-1000+ | Large clusters, varied capacity | Better distribution, more memory |

## Thread Safety

The HashRing implementation is fully thread-safe:

```csharp
var ring = new HashRing<string>();

// Safe concurrent operations
var tasks = new List<Task>();

// Concurrent reads
for (int i = 0; i < 100; i++)
{
    int userId = i;
    tasks.Add(Task.Run(() =>
    {
        var server = ring.GetServer($"user-{userId}");
        // Process user on server
    }));
}

// Concurrent server management
tasks.Add(Task.Run(() => ring.Add("new-server")));
tasks.Add(Task.Run(() => ring.Remove("old-server")));

await Task.WhenAll(tasks);
```

**Thread Safety Features:**
- Lock-free reads for GetServer operations
- Synchronized writes for Add/Remove operations
- Atomic virtual node list updates
- Thread-safe hash algorithm implementations

## Performance Characteristics

### Time Complexity

| Operation | Time Complexity | Notes |
|-----------|----------------|--------|
| `GetServer` | O(log V) | V = total virtual nodes |
| `Add` | O(V) | Rebuilds virtual node list |
| `Remove` | O(V) | Rebuilds virtual node list |
| `Contains` | O(1) | Dictionary lookup |

### Memory Usage

- **Base overhead**: ~100 bytes per server
- **Virtual nodes**: ~32 bytes per virtual node
- **Example**: 10 servers × 42 virtual nodes = ~14KB

### Benchmarks

Typical performance on modern hardware:

| Operation | Throughput | Notes |
|-----------|------------|--------|
| GetServer | 1M+ ops/sec | Single-threaded |
| Concurrent reads | 10M+ ops/sec | Multi-threaded |
| Add/Remove | 100K ops/sec | Write operations |

## Error Handling

### Exception Types

| Exception | When Thrown | Mitigation |
|-----------|-------------|------------|
| `ArgumentNullException` | Null server or key | Validate inputs |
| `ArgumentOutOfRangeException` | Invalid virtual node count | Use positive values |
| `InvalidOperationException` | GetServer on empty ring | Check IsEmpty first |

### Safe Operations

```csharp
// Use TryGetServer for safe operations
if (ring.TryGetServer("user-123", out string server))
{
    // Server found, proceed
    await ProcessUser(server);
}
else
{
    // No servers available
    await HandleNoServers();
}

// Check before operations
if (!ring.IsEmpty)
{
    var server = ring.GetServer("user-123");
}
```

## Best Practices

### Server Naming

```csharp
// Good: Use consistent, descriptive names
ring.Add("api-server-1.us-east-1.example.com");
ring.Add("api-server-2.us-east-1.example.com");

// Avoid: Generic or changing names
ring.Add("server1");  // What if server1 changes meaning?
```

### Capacity Planning

```csharp
// Assign virtual nodes based on server capacity
ring.Add("small-server", 21);   // 2 CPU, 4GB RAM
ring.Add("medium-server", 42);  // 4 CPU, 8GB RAM
ring.Add("large-server", 84);   // 8 CPU, 16GB RAM
```

### Graceful Degradation

```csharp
public async Task<T> CallWithFailover<T>(string key, Func<string, Task<T>> operation)
{
    var servers = ring.GetServers(key, 3);

    foreach (var server in servers)
    {
        try
        {
            return await operation(server);
        }
        catch (Exception ex) when (IsTransientError(ex))
        {
            // Log and try next server
            _logger.LogWarning("Server {Server} failed: {Error}", server, ex.Message);
            continue;
        }
    }

    throw new AllServersUnavailableException();
}
```

### Monitoring

```csharp
// Track distribution
var distribution = new Dictionary<string, int>();
foreach (var server in ring.Servers)
    distribution[server] = 0;

// Sample requests
for (int i = 0; i < 10000; i++)
{
    var key = $"user-{i}";
    var server = ring.GetServer(key);
    distribution[server]++;
}

// Analyze balance
foreach (var kvp in distribution)
{
    var percentage = (kvp.Value * 100.0) / 10000;
    Console.WriteLine($"{kvp.Key}: {percentage:F1}%");
}
```

## Migration and Deployment

### Adding New Servers

```csharp
// 1. Add server to ring
ring.Add("new-server.example.com");

// 2. Some keys will automatically route to new server
// 3. Monitor redistribution and performance
```

### Removing Servers

```csharp
// 1. Stop routing new requests (optional)
// 2. Remove from ring
bool removed = ring.Remove("old-server.example.com");

// 3. Keys will redistribute to remaining servers
// 4. Handle any in-flight requests to removed server
```

### Rolling Updates

```csharp
// Gradual server replacement
foreach (var oldServer in serversToReplace)
{
    // Add replacement
    ring.Add(GetReplacementServer(oldServer));

    // Wait for traffic to stabilize
    await Task.Delay(TimeSpan.FromMinutes(1));

    // Remove old server
    ring.Remove(oldServer);

    // Monitor and validate
    await ValidateDistribution();
}
```

## Integration Examples

See the complete examples in the samples folder:

- **Database Routing**: `samples/Common.Utilities.Samples/ConsistentHashing/DatabaseRoutingExample.cs`
- **HTTP Load Balancing**: `samples/Common.Utilities.Samples/ConsistentHashing/HttpRoutingExample.cs`

These examples demonstrate real-world usage patterns and best practices for production deployments.

## Troubleshooting

### Common Issues

**Uneven Distribution**
- Increase virtual nodes count
- Check server naming consistency
- Verify hash algorithm choice

**Performance Issues**
- Profile GetServer calls
- Consider caching for repeated keys
- Monitor virtual node count vs. memory usage

**Failover Problems**
- Implement retry logic with GetServers()
- Add health checking
- Use TryGetServer() for graceful handling

### Debug Information

```csharp
// Ring status
Console.WriteLine($"Servers: {ring.Servers.Count}");
Console.WriteLine($"Virtual Nodes: {ring.VirtualNodeCount}");
Console.WriteLine($"Is Empty: {ring.IsEmpty}");

// Server list
foreach (var server in ring.Servers)
{
    Console.WriteLine($"  - {server}");
}
```

## Related Documentation

- [Extension Methods](extension-methods.md) - General extension method utilities
- [Synchronization Utilities](synchronization-utilities.md) - Thread-safe utilities
- [Global Abstractions](global-abstractions.md) - Common interfaces and patterns
