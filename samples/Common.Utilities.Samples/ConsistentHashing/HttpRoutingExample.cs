using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.Samples.ConsistentHashing;

/// <summary>
/// Example demonstrating how to use consistent hashing for HTTP load balancing.
/// This shows how to distribute HTTP requests across multiple backend servers.
/// </summary>
public static class HttpRoutingExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== HTTP Load Balancing with Consistent Hashing ===");

        // Setup: Create hash ring with backend servers
        var loadBalancer = new HttpLoadBalancer();

        // Add backend servers with different capacities (virtual nodes)
        loadBalancer.AddServer("api-server-1.example.com", 100); // Small server
        loadBalancer.AddServer("api-server-2.example.com", 200); // Medium server
        loadBalancer.AddServer("api-server-3.example.com", 300); // Large server
        loadBalancer.AddServer("api-server-4.example.com", 200); // Medium server

        Console.WriteLine("Configured backend servers:");
        foreach (var server in loadBalancer.GetAllServers())
        {
            Console.WriteLine($"  - {server}");
        }
        Console.WriteLine();

        // Simulate routing requests based on session IDs
        var sessionIds = new[]
        {
            "session-abc123", "session-def456", "session-ghi789",
            "session-jkl012", "session-mno345", "session-pqr678",
            "session-stu901", "session-vwx234"
        };

        Console.WriteLine("Session ID to Backend Server routing:");
        foreach (var sessionId in sessionIds)
        {
            var server = loadBalancer.RouteRequest(sessionId);
            Console.WriteLine($"  {sessionId} -> {server}");
        }
        Console.WriteLine();

        // Demonstrate sticky sessions - same session always goes to same server
        Console.WriteLine("Sticky session consistency check:");
        const string testSession = "session-abc123";
        for (int i = 0; i < 5; i++)
        {
            var server = loadBalancer.RouteRequest(testSession);
            Console.WriteLine($"  Request {i + 1}: {testSession} -> {server}");
        }
        Console.WriteLine();

        // Simulate server maintenance and removal
        Console.WriteLine("=== Server Maintenance Simulation ===");
        const string serverToRemove = "api-server-2.example.com";
        Console.WriteLine($"Taking server offline for maintenance: {serverToRemove}");
        loadBalancer.RemoveServer(serverToRemove);

        Console.WriteLine("Routing during maintenance:");
        foreach (var sessionId in sessionIds)
        {
            var server = loadBalancer.RouteRequest(sessionId);
            Console.WriteLine($"  {sessionId} -> {server}");
        }
        Console.WriteLine();

        // Server comes back online
        Console.WriteLine("=== Server Back Online ===");
        Console.WriteLine($"Bringing server back online: {serverToRemove}");
        loadBalancer.AddServer(serverToRemove, 200); // Medium capacity

        Console.WriteLine("Routing after server recovery:");
        foreach (var sessionId in sessionIds)
        {
            var server = loadBalancer.RouteRequest(sessionId);
            Console.WriteLine($"  {sessionId} -> {server}");
        }
        Console.WriteLine();

        // Show load distribution across servers
        Console.WriteLine("=== Load Distribution Analysis ===");
        var distribution = loadBalancer.AnalyzeDistribution(10000);

        Console.WriteLine("Distribution of 10,000 requests across servers:");
        foreach (var kvp in distribution)
        {
            var percentage = (kvp.Value * 100.0) / 10000;
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} requests ({percentage:F1}%)");
        }
        Console.WriteLine();

        // Advanced routing example with user preferences
        Console.WriteLine("=== Advanced Routing with User Context ===");
        var advancedRouter = new AdvancedHttpRouter();

        // Add servers with geographic regions
        advancedRouter.AddServer(new ServerInfo("us-east-1.api.example.com", "US-East", 200));
        advancedRouter.AddServer(new ServerInfo("us-west-1.api.example.com", "US-West", 200));
        advancedRouter.AddServer(new ServerInfo("eu-west-1.api.example.com", "EU-West", 150));

        // Route requests based on user ID and preferences
        var requests = new[]
        {
            new UserRequest("user-001", "US", "/api/orders"),
            new UserRequest("user-002", "EU", "/api/products"),
            new UserRequest("user-003", "US", "/api/profile"),
            new UserRequest("user-004", "EU", "/api/search"),
        };

        Console.WriteLine("Advanced routing with geographic preference:");
        foreach (var request in requests)
        {
            var server = advancedRouter.RouteRequest(request);
            Console.WriteLine($"  {request.UserId} ({request.Region}) -> {server.Hostname} ({server.Region})");
        }

        Console.WriteLine("\n=== HTTP Load Balancing Example Complete ===\n");
    }
}

/// <summary>
/// Simple HTTP load balancer using consistent hashing for sticky sessions.
/// </summary>
public class HttpLoadBalancer
{
    private readonly HashRing<string> _ring;

    public HttpLoadBalancer()
    {
        _ring = new HashRing<string>();
    }

    public void AddServer(string serverHostname, int capacity = 42)
    {
        _ring.Add(serverHostname, capacity);
    }

    public bool RemoveServer(string serverHostname)
    {
        return _ring.Remove(serverHostname);
    }

    public string RouteRequest(string sessionId)
    {
        if (String.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

        return _ring.GetServer(sessionId);
    }

    public bool TryRouteRequest(string sessionId, out string? server)
    {
        server = null;

        if (String.IsNullOrEmpty(sessionId))
            return false;

        return _ring.TryGetServer(sessionId, out server);
    }

    public IReadOnlyCollection<string> GetAllServers()
    {
        return _ring.Servers;
    }

    public Dictionary<string, int> AnalyzeDistribution(int sampleSize)
    {
        var distribution = new Dictionary<string, int>();

        foreach (var server in _ring.Servers)
        {
            distribution[server] = 0;
        }

        // Generate sample requests
        var random = new Random(42); // Fixed seed for reproducible results
        for (int i = 0; i < sampleSize; i++)
        {
            var sessionId = $"session-{random.Next(100000):D6}";
            if (_ring.TryGetServer(sessionId, out var server) && server != null)
            {
                distribution[server]++;
            }
        }

        return distribution;
    }
}

/// <summary>
/// Advanced HTTP router that considers server metadata.
/// </summary>
public class AdvancedHttpRouter
{
    private readonly HashRing<ServerInfo> _ring;

    public AdvancedHttpRouter()
    {
        _ring = new HashRing<ServerInfo>();
    }

    public void AddServer(ServerInfo serverInfo, int capacity = 42)
    {
        _ring.Add(serverInfo, capacity);
    }

    public bool RemoveServer(ServerInfo serverInfo)
    {
        return _ring.Remove(serverInfo);
    }

    public ServerInfo RouteRequest(UserRequest request)
    {
        // Create a routing key that includes both user ID and some context
        var routingKey = $"{request.UserId}:{request.Region}";
        return _ring.GetServer(routingKey);
    }

    public IReadOnlyCollection<ServerInfo> GetAllServers()
    {
        return _ring.Servers;
    }
}

/// <summary>
/// Represents server information including hostname and region.
/// </summary>
public record ServerInfo(string Hostname, string Region, int Capacity) : IEquatable<ServerInfo>
{
    public override string ToString() => $"{Hostname} ({Region})";
}

/// <summary>
/// Represents a user request with context information.
/// </summary>
public record UserRequest(string UserId, string Region, string Path);
