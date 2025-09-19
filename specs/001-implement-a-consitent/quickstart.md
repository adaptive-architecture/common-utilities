# QuickStart: Consistent Hash Ring

## Installation
```bash
dotnet add package AdaptArch.Common.Utilities
```

## Basic Usage

### 1. Simple Server Routing
```csharp
using AdaptArch.Common.Utilities.ConsistentHashing;

// Create hash ring with string servers
var ring = new HashRing<string>();

// Add servers
ring.Add("server1.example.com");
ring.Add("server2.example.com");
ring.Add("server3.example.com");

// Route requests
string server = ring.GetServer("user123");
Console.WriteLine($"Route user123 to: {server}");

// Same key always routes to same server
string sameServer = ring.GetServer("user123");
Debug.Assert(server == sameServer);
```

### 2. Database Connection Routing
```csharp
// Define database connection info
public record DatabaseServer(string Host, int Port, string Database);

// Create ring with custom server type
var dbRing = new HashRing<DatabaseServer>();

// Add database servers
dbRing.Add(new DatabaseServer("db1.example.com", 5432, "shard1"));
dbRing.Add(new DatabaseServer("db2.example.com", 5432, "shard2"));
dbRing.Add(new DatabaseServer("db3.example.com", 5432, "shard3"));

// Route database operations
var userId = Guid.NewGuid();
DatabaseServer dbServer = dbRing.GetServer(userId);
Console.WriteLine($"User {userId} -> {dbServer.Host}:{dbServer.Port}/{dbServer.Database}");
```

### 3. HTTP Request Load Balancing
```csharp
// Define HTTP endpoint
public class HttpEndpoint : IEquatable<HttpEndpoint>
{
    public string Url { get; init; }
    public string Region { get; init; }

    public bool Equals(HttpEndpoint other) => Url == other?.Url;
    public override int GetHashCode() => Url.GetHashCode();
}

// Create ring with HTTP endpoints
var httpRing = new HashRing<HttpEndpoint>();

httpRing.Add(new HttpEndpoint { Url = "https://api-east.example.com", Region = "us-east-1" });
httpRing.Add(new HttpEndpoint { Url = "https://api-west.example.com", Region = "us-west-1" });
httpRing.Add(new HttpEndpoint { Url = "https://api-eu.example.com", Region = "eu-west-1" });

// Route HTTP requests
string requestId = "req_" + DateTimeOffset.UtcNow.Ticks;
HttpEndpoint endpoint = httpRing.GetServer(requestId);
Console.WriteLine($"Route {requestId} to {endpoint.Url} in {endpoint.Region}");
```

## Advanced Configuration

### 4. Custom Virtual Node Count
```csharp
// Default is 42 virtual nodes per server (good distribution)
var ring = new HashRing<string>();

// Increase for even better distribution
var distributedRing = new HashRing<string>(virtualNodes: 1000);

// Or specify per server
ring.Add("powerful-server", virtualNodes: 800);
ring.Add("small-server", virtualNodes: 200);
```

### 5. Custom Hash Algorithm
```csharp
// Use MD5 for faster hashing (less secure)
var fastRing = new HashRing<string>(new Md5HashAlgorithm());

// Or keep default SHA1 for better distribution
var secureRing = new HashRing<string>(new Sha1HashAlgorithm());
```

### 6. Safe Operations
```csharp
var ring = new HashRing<string>();
ring.Add("server1");

// Safe lookup - won't throw on empty ring
if (ring.TryGetServer("key123", out string server))
{
    Console.WriteLine($"Found server: {server}");
}
else
{
    Console.WriteLine("Ring is empty");
}
```

## Dynamic Scaling Examples

### 7. Adding Servers (Minimal Redistribution)
```csharp
var ring = new HashRing<string>();
ring.Add("server1");
ring.Add("server2");
ring.Add("server3");

// Record current mappings
var testKeys = new[] { "user1", "user2", "user3", "user4", "user5" };
var beforeMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

// Add new server - only some keys will be redistributed
ring.Add("server4");

// Check redistribution
var afterMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
int redistributed = testKeys.Count(key => beforeMapping[key] != afterMapping[key]);

Console.WriteLine($"Added server4: {redistributed}/{testKeys.Length} keys redistributed");
// With 42 virtual nodes: expect ~25% redistribution with good balance
// Higher virtual nodes provide more even redistribution
```

### 8. Handling Server Failures
```csharp
var ring = new HashRing<string>();
ring.Add("server1");
ring.Add("server2");
ring.Add("server3");

string key = "important-data";
string originalServer = ring.GetServer(key);
Console.WriteLine($"Original: {key} -> {originalServer}");

// Simulate server failure
ring.Remove(originalServer);

// Requests automatically failover to next server
string failoverServer = ring.GetServer(key);
Console.WriteLine($"Failover: {key} -> {failoverServer}");

// When original server returns, add it back
ring.Add(originalServer);
string restoredServer = ring.GetServer(key);
Console.WriteLine($"Restored: {key} -> {restoredServer}");
```

## Thread Safety Example

### 9. Concurrent Access
```csharp
var ring = new HashRing<string>();
ring.Add("server1");
ring.Add("server2");
ring.Add("server3");

// Safe concurrent reads
var tasks = Enumerable.Range(0, 1000).Select(i =>
    Task.Run(() => {
        string key = $"user{i}";
        string server = ring.GetServer(key);
        return (key, server);
    })
).ToArray();

var results = await Task.WhenAll(tasks);

// Concurrent read/write (writes are coordinated)
var writeTask = Task.Run(() => {
    Thread.Sleep(100);
    ring.Add("server4"); // Safe to add while others read
});

await writeTask;
```

## Performance Monitoring

### 10. Ring Statistics
```csharp
var ring = new HashRing<string>();
ring.Add("server1");
ring.Add("server2", 600); // More virtual nodes
ring.Add("server3", 200); // Fewer virtual nodes

Console.WriteLine($"Servers: {ring.Servers.Count}");
Console.WriteLine($"Virtual nodes: {ring.VirtualNodeCount}");
Console.WriteLine($"Is empty: {ring.IsEmpty}");

// Test distribution
var keys = Enumerable.Range(0, 10000).Select(i => $"key{i}").ToArray();
var distribution = keys.GroupBy(key => ring.GetServer(key))
                      .ToDictionary(g => g.Key, g => g.Count());

foreach (var (server, count) in distribution)
{
    double percentage = (count * 100.0) / keys.Length;
    Console.WriteLine($"{server}: {count} keys ({percentage:F1}%)");
}
```

## Integration with Dependency Injection

### 11. ASP.NET Core Integration
```csharp
// Startup.cs or Program.cs
services.Configure<HashRingOptions>(options => {
    options.DefaultVirtualNodes = 500;  // Increase from default 42 for better distribution
    options.HashAlgorithm = new Sha1HashAlgorithm();
});

services.AddSingleton<HashRing<string>>();

// In your controller or service
public class ApiController : ControllerBase
{
    private readonly HashRing<string> _ring;

    public ApiController(HashRing<string> ring)
    {
        _ring = ring;
    }

    public IActionResult RouteRequest(string userId)
    {
        string server = _ring.GetServer(userId);
        return Ok(new { userId, server });
    }
}
```

## Testing Your Hash Ring

### 12. Unit Test Example
```csharp
[Test]
public void HashRing_ConsistentMapping_SameKeyReturnsServer()
{
    // Arrange
    var ring = new HashRing<string>();
    ring.Add("server1");
    ring.Add("server2");

    // Act
    string server1 = ring.GetServer("testkey");
    string server2 = ring.GetServer("testkey");

    // Assert
    Assert.AreEqual(server1, server2, "Same key should map to same server");
}

[Test]
public void HashRing_MinimalRedistribution_OnServerAddition()
{
    // Arrange
    var ring = new HashRing<string>();
    ring.Add("server1");
    ring.Add("server2");

    var keys = Enumerable.Range(0, 1000).Select(i => $"key{i}").ToArray();
    var beforeMapping = keys.ToDictionary(k => k, k => ring.GetServer(k));

    // Act
    ring.Add("server3");
    var afterMapping = keys.ToDictionary(k => k, k => ring.GetServer(k));

    // Assert
    int redistributed = keys.Count(k => beforeMapping[k] != afterMapping[k]);
    double redistributionRatio = redistributed / (double)keys.Length;

    Assert.Less(redistributionRatio, 0.5, "Less than 50% of keys should be redistributed");
}
```

This quickstart demonstrates the key features and usage patterns for the consistent hash ring utility. The examples progress from simple usage to advanced scenarios including dynamic scaling, thread safety, and integration patterns.
