# Leader Election - Advanced Configuration

This guide covers advanced configuration options, performance tuning, custom implementations, and Redis-specific optimizations for the leader election utilities.

## Advanced Configuration Options

### Fine-Tuning Timing Parameters

The timing configuration directly impacts failover speed, network traffic, and system stability. Here's how to optimize for different scenarios:

#### High-Availability Configuration

```csharp
var highAvailabilityOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromSeconds(60),     // 1 minute lease
    RenewalInterval = TimeSpan.FromSeconds(15),   // Renew every 15 seconds
    RetryInterval = TimeSpan.FromSeconds(5),      // Quick retry for followers
    OperationTimeout = TimeSpan.FromSeconds(10),  // Fast timeout detection
    AutoStart = true
};

// This configuration provides:
// - Fast failover (within 15-60 seconds)
// - Higher network traffic (more frequent operations)
// - Better fault tolerance (quick detection of failures)
```

#### Low-Traffic Configuration

```csharp
var lowTrafficOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(15),     // 15 minute lease
    RenewalInterval = TimeSpan.FromMinutes(5),    // Renew every 5 minutes
    RetryInterval = TimeSpan.FromMinutes(2),      // Slow retry for followers
    OperationTimeout = TimeSpan.FromMinutes(1),   // Longer timeout
    AutoStart = true
};

// This configuration provides:
// - Slow failover (5-15 minutes)
// - Lower network traffic (fewer operations)
// - More stable for networks with occasional hiccups
```

#### Performance-Critical Configuration

```csharp
var performanceOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(5),      // Medium lease duration
    RenewalInterval = TimeSpan.FromMinutes(2),    // Infrequent renewal
    RetryInterval = TimeSpan.FromSeconds(30),     // Moderate retry frequency
    OperationTimeout = TimeSpan.FromSeconds(15),  // Quick operation timeout
    AutoStart = false                             // Manual control for performance
};

// This configuration provides:
// - Balanced failover (2-5 minutes)
// - Moderate network traffic
// - Manual control for performance-critical sections
```

### Custom Metadata Configuration

Metadata allows you to attach additional information to leadership leases:

```csharp
var metadataOptions = new LeaderElectionOptions
{
    Metadata = new Dictionary<string, string>
    {
        ["Version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
        ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
        ["MachineName"] = Environment.MachineName,
        ["ProcessId"] = Environment.ProcessId.ToString(),
        ["StartTime"] = DateTime.UtcNow.ToString("O"),
        ["Region"] = GetCurrentRegion(),
        ["Features"] = string.Join(",", GetEnabledFeatures())
    }
};

await using var service = new InProcessLeaderElectionService(
    "metadata-election",
    Environment.MachineName,
    metadataOptions);

// Access metadata from leadership events
service.LeadershipChanged += (sender, args) =>
{
    if (args.CurrentLeader?.Metadata != null)
    {
        Console.WriteLine($"Leader metadata:");
        foreach (var (key, value) in args.CurrentLeader.Metadata)
        {
            Console.WriteLine($"  {key}: {value}");
        }
    }
};
```

### Environment-Specific Configuration

```csharp
public static class LeaderElectionConfiguration
{
    public static LeaderElectionOptions GetOptions(string environment)
    {
        return environment.ToLower() switch
        {
            "development" => new LeaderElectionOptions
            {
                LeaseDuration = TimeSpan.FromMinutes(2),
                RenewalInterval = TimeSpan.FromSeconds(30),
                RetryInterval = TimeSpan.FromSeconds(10),
                OperationTimeout = TimeSpan.FromSeconds(15),
                AutoStart = true
            },
            "staging" => new LeaderElectionOptions
            {
                LeaseDuration = TimeSpan.FromMinutes(5),
                RenewalInterval = TimeSpan.FromMinutes(1),
                RetryInterval = TimeSpan.FromSeconds(30),
                OperationTimeout = TimeSpan.FromSeconds(30),
                AutoStart = true
            },
            "production" => new LeaderElectionOptions
            {
                LeaseDuration = TimeSpan.FromMinutes(10),
                RenewalInterval = TimeSpan.FromMinutes(3),
                RetryInterval = TimeSpan.FromMinutes(1),
                OperationTimeout = TimeSpan.FromMinutes(1),
                AutoStart = true
            },
            _ => new LeaderElectionOptions() // Default
        };
    }
}

// Usage
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var options = LeaderElectionConfiguration.GetOptions(environment);
```

## Custom Lease Store Implementation

While the package includes `InProcessLeaseStore` for single-application scenarios, you can implement custom lease stores for distributed systems:

### Database-Backed Lease Store

```csharp
public class DatabaseLeaseStore : ILeaseStore
{
    private readonly IDbConnection _connection;
    private readonly ILogger _logger;

    public DatabaseLeaseStore(IDbConnection connection, ILogger logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName, 
        string participantId, 
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTime.UtcNow.Add(leaseDuration);
        var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;

        const string sql = @"
            INSERT INTO LeaderElection (ElectionName, ParticipantId, AcquiredAt, ExpiresAt, Metadata)
            SELECT @ElectionName, @ParticipantId, @AcquiredAt, @ExpiresAt, @Metadata
            WHERE NOT EXISTS (
                SELECT 1 FROM LeaderElection 
                WHERE ElectionName = @ElectionName AND ExpiresAt > @Now
            )";

        try
        {
            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                ElectionName = electionName,
                ParticipantId = participantId,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Metadata = metadataJson,
                Now = DateTime.UtcNow
            });

            if (rowsAffected > 0)
            {
                return new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire lease for {ElectionName}:{ParticipantId}", 
                electionName, participantId);
            return null;
        }
    }

    public async Task<LeaderInfo?> TryRenewLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTime.UtcNow.Add(leaseDuration);
        var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;

        const string sql = @"
            UPDATE LeaderElection 
            SET ExpiresAt = @ExpiresAt, Metadata = @Metadata
            WHERE ElectionName = @ElectionName 
                AND ParticipantId = @ParticipantId 
                AND ExpiresAt > @Now";

        try
        {
            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                ExpiresAt = expiresAt,
                Metadata = metadataJson,
                ElectionName = electionName,
                ParticipantId = participantId,
                Now = DateTime.UtcNow
            });

            if (rowsAffected > 0)
            {
                var acquiredAt = await _connection.QuerySingleAsync<DateTime>(
                    "SELECT AcquiredAt FROM LeaderElection WHERE ElectionName = @ElectionName AND ParticipantId = @ParticipantId",
                    new { ElectionName = electionName, ParticipantId = participantId });

                return new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = acquiredAt,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew lease for {ElectionName}:{ParticipantId}", 
                electionName, participantId);
            return null;
        }
    }

    public async Task<bool> ReleaseLeaseAsync(
        string electionName,
        string participantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM LeaderElection 
            WHERE ElectionName = @ElectionName AND ParticipantId = @ParticipantId";

        try
        {
            await _connection.ExecuteAsync(sql, new
            {
                ElectionName = electionName,
                ParticipantId = participantId
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release lease for {ElectionName}:{ParticipantId}", 
                electionName, participantId);
            return false;
        }
    }

    public async Task<LeaderInfo?> GetCurrentLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT ParticipantId, AcquiredAt, ExpiresAt, Metadata
            FROM LeaderElection
            WHERE ElectionName = @ElectionName AND ExpiresAt > @Now";

        try
        {
            var result = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql, new
            {
                ElectionName = electionName,
                Now = DateTime.UtcNow
            });

            if (result == null) return null;

            var metadata = result.Metadata != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(result.Metadata)
                : null;

            return new LeaderInfo
            {
                ParticipantId = result.ParticipantId,
                AcquiredAt = result.AcquiredAt,
                ExpiresAt = result.ExpiresAt,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current lease for {ElectionName}", electionName);
            return null;
        }
    }

    public async Task<bool> HasValidLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1) FROM LeaderElection
            WHERE ElectionName = @ElectionName AND ExpiresAt > @Now";

        try
        {
            var count = await _connection.QuerySingleAsync<int>(sql, new
            {
                ElectionName = electionName,
                Now = DateTime.UtcNow
            });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check lease validity for {ElectionName}", electionName);
            return false;
        }
    }
}
```

### Redis-Based Lease Store (Available in Common.Utilities.Redis)

The Redis implementation provides distributed leader election capabilities using atomic Redis operations. Here's an example of how the built-in `RedisLeaseStore` works:

```csharp
using AdaptArch.Common.Utilities.Redis.LeaderElection;
using StackExchange.Redis;

// Using the built-in Redis implementation
var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
var serializer = new ReflectionJsonDataSerializer();

// Create a Redis-based leader election service directly
var leaderService = new RedisLeaderElectionService(
    connectionMultiplexer,
    serializer,
    "distributed-election",
    Environment.MachineName);

// Or if you need to use the lease store directly for custom scenarios
var redisLeaseStore = new RedisLeaseStore(
    connectionMultiplexer.GetDatabase(),
    serializer);
```

#### Key Features of Redis Implementation

**Atomic Operations**: Uses Redis SET with NX (only if not exists) and EX (expiration) for lease acquisition:
```csharp
// Simplified example of atomic acquisition
bool acquired = await database.StringSetAsync(
    "leader-election:my-service",
    serializedLeaseData,
    TimeSpan.FromMinutes(5),
    When.NotExists);
```

**Lua Scripts for Consistency**: Renewal and release operations use Lua scripts for atomic operations:
```lua
-- Lease renewal script
local key = KEYS[1]
local participant = ARGV[1]
local duration = ARGV[2]
local newData = ARGV[3]

local current = redis.call('GET', key)
if current then
    local data = cjson.decode(current)
    if data.ParticipantId == participant then
        redis.call('SETEX', key, duration, newData)
        return newData
    end
end
return nil
```

**Automatic Expiration**: Leverages Redis TTL for automatic lease expiration without cleanup tasks.

**JSON Serialization**: Uses configurable serialization for lease data and metadata.

#### Redis Configuration Examples

**Basic Redis Setup**:
```csharp
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var serializer = new ReflectionJsonDataSerializer();

var options = new RedisLeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(5),
    RenewalInterval = TimeSpan.FromMinutes(2),
    RetryInterval = TimeSpan.FromSeconds(30),
    AutoStart = true
};

var service = new RedisLeaderElectionService(
    redis,
    serializer,
    "my-distributed-service",
    Environment.MachineName,
    options);
```

**Redis Cluster Setup**:
```csharp
var clusterOptions = new ConfigurationOptions
{
    EndPoints = {
        { "redis-node1.company.com", 7000 },
        { "redis-node2.company.com", 7000 },
        { "redis-node3.company.com", 7000 }
    },
    AbortOnConnectFail = false,
    ConnectTimeout = 5000,
    SyncTimeout = 5000
};

var redis = ConnectionMultiplexer.Connect(clusterOptions);
var serializer = new ReflectionJsonDataSerializer();

var service = new RedisLeaderElectionService(
    redis,
    serializer,
    "cluster-service",
    $"{Environment.MachineName}-{Environment.ProcessId}");
```

**Redis Sentinel Setup**:
```csharp
var sentinelOptions = new ConfigurationOptions
{
    EndPoints = {
        { "sentinel1.company.com", 26379 },
        { "sentinel2.company.com", 26379 },
        { "sentinel3.company.com", 26379 }
    },
    ServiceName = "mymaster",
    TieBreaker = "",
    CommandMap = CommandMap.Sentinel
};

var redis = ConnectionMultiplexer.Connect(sentinelOptions);
var service = new RedisLeaderElectionService(
    redis,
    new ReflectionJsonDataSerializer(),
    "sentinel-service",
    Environment.MachineName);
```

## Performance Tuning

### Redis-Specific Performance Optimizations

#### Connection Pool Configuration
```csharp
var redisOptions = new ConfigurationOptions
{
    EndPoints = { "redis.company.com:6379" },
    ConnectTimeout = 5000,
    SyncTimeout = 5000,
    AsyncTimeout = 5000,
    
    // Connection pool settings
    ChannelMultiplexer = null,
    DefaultDatabase = 0,
    ConnectRetry = 3,
    ReconnectRetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1)),
    
    // Performance optimizations
    AbortOnConnectFail = false,
    AllowAdmin = false,
    KeepAlive = 60,
    
    // Redis-specific tuning
    ResponseTimeout = TimeSpan.FromSeconds(5),
    CommandMap = CommandMap.Default
};

var redis = ConnectionMultiplexer.Connect(redisOptions);
```

#### Optimized Timing for Distributed Systems
```csharp
public static class RedisLeaderElectionTimingProfiles
{
    /// <summary>
    /// Optimized for low-latency networks (same datacenter)
    /// </summary>
    public static RedisLeaderElectionOptions LowLatency() => new()
    {
        LeaseDuration = TimeSpan.FromMinutes(2),      // Short lease for quick failover
        RenewalInterval = TimeSpan.FromSeconds(30),   // Frequent renewal
        RetryInterval = TimeSpan.FromSeconds(10),     // Quick retry
        OperationTimeout = TimeSpan.FromSeconds(5),   // Fast timeout
        AutoStart = true,
        KeyPrefix = "leader-election"
    };

    /// <summary>
    /// Optimized for high-latency networks (cross-region)
    /// </summary>
    public static RedisLeaderElectionOptions HighLatency() => new()
    {
        LeaseDuration = TimeSpan.FromMinutes(10),     // Longer lease to handle network delays
        RenewalInterval = TimeSpan.FromMinutes(3),    // Less frequent renewal
        RetryInterval = TimeSpan.FromMinutes(1),      // Slower retry
        OperationTimeout = TimeSpan.FromSeconds(30),  // Longer timeout
        AutoStart = true,
        KeyPrefix = "leader-election"
    };

    /// <summary>
    /// Optimized for cost-sensitive scenarios (minimize Redis operations)
    /// </summary>
    public static RedisLeaderElectionOptions CostOptimized() => new()
    {
        LeaseDuration = TimeSpan.FromMinutes(30),     // Long lease
        RenewalInterval = TimeSpan.FromMinutes(10),   // Infrequent renewal
        RetryInterval = TimeSpan.FromMinutes(5),      // Slow retry
        OperationTimeout = TimeSpan.FromSeconds(15),  // Reasonable timeout
        AutoStart = true,
        KeyPrefix = "leader-election"
    };
}

// Usage
var options = RedisLeaderElectionTimingProfiles.LowLatency();
var service = new RedisLeaderElectionService(redis, serializer, "my-service", participantId, options);
```

#### Redis Key Management and Namespace Isolation
```csharp
var options = new RedisLeaderElectionOptions
{
    KeyPrefix = "myapp:leader-election",  // Namespace your keys
    LeaseDuration = TimeSpan.FromMinutes(5),
    RenewalInterval = TimeSpan.FromMinutes(2),
    RetryInterval = TimeSpan.FromSeconds(30),
    AutoStart = true
};

// This will create keys like: "myapp:leader-election:service-name"
var service = new RedisLeaderElectionService(redis, serializer, "service-name", participantId, options);
```

#### Redis Monitoring and Diagnostics
```csharp
public class RedisLeaderElectionMonitor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public RedisLeaderElectionMonitor(IConnectionMultiplexer redis, ILogger logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RedisHealthInfo> CheckRedisHealthAsync()
    {
        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);

            var pingTime = await database.PingAsync();
            var info = await server.InfoAsync();
            var clientCount = info.FirstOrDefault(x => x.Key == "connected_clients")?.Value ?? "0";

            return new RedisHealthInfo
            {
                IsHealthy = true,
                PingTime = pingTime,
                ConnectedClients = int.Parse(clientCount),
                ServerInfo = info.ToDictionary(x => x.Key, x => x.Value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return new RedisHealthInfo { IsHealthy = false, Error = ex.Message };
        }
    }

    public async Task<List<string>> GetActiveElectionsAsync(string keyPrefix = "leader-election")
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var pattern = $"{keyPrefix}:*";
            var keys = server.Keys(pattern: pattern);
            
            return keys.Select(key => key.ToString().Replace($"{keyPrefix}:", "")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active elections");
            return new List<string>();
        }
    }
}

public class RedisHealthInfo
{
    public bool IsHealthy { get; set; }
    public TimeSpan PingTime { get; set; }
    public int ConnectedClients { get; set; }
    public Dictionary<string, string> ServerInfo { get; set; } = new();
    public string? Error { get; set; }
}
```

### Monitoring and Metrics

```csharp
public class MetricsCollectingLeaderElectionService : ILeaderElectionService
{
    private readonly ILeaderElectionService _inner;
    private readonly IMetricsCollector _metrics;
    private readonly ILogger _logger;

    public MetricsCollectingLeaderElectionService(
        ILeaderElectionService inner,
        IMetricsCollector metrics,
        ILogger logger)
    {
        _inner = inner;
        _metrics = metrics;
        _logger = logger;

        // Forward events and collect metrics
        _inner.LeadershipChanged += OnLeadershipChanged;
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            _metrics.Increment("leader_election.leadership_gained");
            _metrics.Gauge("leader_election.is_leader", 1, new[] { $"election:{_inner.ElectionName}" });
            _logger.LogInformation("Leadership gained for {ElectionName}:{ParticipantId}", 
                _inner.ElectionName, _inner.ParticipantId);
        }
        else if (args.LeadershipLost)
        {
            _metrics.Increment("leader_election.leadership_lost");
            _metrics.Gauge("leader_election.is_leader", 0, new[] { $"election:{_inner.ElectionName}" });
            _logger.LogInformation("Leadership lost for {ElectionName}:{ParticipantId}", 
                _inner.ElectionName, _inner.ParticipantId);
        }

        // Forward the event
        LeadershipChanged?.Invoke(sender, args);
    }

    public async Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _metrics.StartTimer("leader_election.acquire_duration");
        
        try
        {
            var result = await _inner.TryAcquireLeadershipAsync(cancellationToken);
            
            _metrics.Increment("leader_election.acquire_attempts");
            if (result)
            {
                _metrics.Increment("leader_election.acquire_success");
            }
            else
            {
                _metrics.Increment("leader_election.acquire_failure");
            }

            return result;
        }
        catch (Exception ex)
        {
            _metrics.Increment("leader_election.acquire_error");
            _logger.LogError(ex, "Error acquiring leadership");
            throw;
        }
    }

    // Forward all other members to the inner service
    public string ParticipantId => _inner.ParticipantId;
    public string ElectionName => _inner.ElectionName;
    public bool IsLeader => _inner.IsLeader;
    public LeaderInfo? CurrentLeader => _inner.CurrentLeader;
    public event EventHandler<LeadershipChangedEventArgs>? LeadershipChanged;

    public Task StartAsync(CancellationToken cancellationToken = default) => 
        _inner.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) => 
        _inner.StopAsync(cancellationToken);

    public Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default) => 
        _inner.ReleaseLeadershipAsync(cancellationToken);

    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
```

### Optimized Timing Strategies

```csharp
public static class TimingOptimizations
{
    /// <summary>
    /// Configuration optimized for microservices with frequent deployments
    /// </summary>
    public static LeaderElectionOptions ForMicroservices() => new()
    {
        LeaseDuration = TimeSpan.FromMinutes(3),      // Balance between stability and quick failover
        RenewalInterval = TimeSpan.FromSeconds(45),   // Frequent enough to detect failures
        RetryInterval = TimeSpan.FromSeconds(15),     // Quick retry for new instances
        OperationTimeout = TimeSpan.FromSeconds(20),  // Reasonable timeout for network operations
        AutoStart = true
    };

    /// <summary>
    /// Configuration optimized for batch processing systems
    /// </summary>
    public static LeaderElectionOptions ForBatchProcessing() => new()
    {
        LeaseDuration = TimeSpan.FromMinutes(30),     // Long-running operations
        RenewalInterval = TimeSpan.FromMinutes(10),   // Infrequent renewal
        RetryInterval = TimeSpan.FromMinutes(5),      // Slow retry for batch systems
        OperationTimeout = TimeSpan.FromMinutes(2),   // Longer timeout for batch operations
        AutoStart = false                             // Manual control for batch jobs
    };

    /// <summary>
    /// Configuration optimized for high-frequency trading systems
    /// </summary>
    public static LeaderElectionOptions ForHighFrequency() => new()
    {
        LeaseDuration = TimeSpan.FromSeconds(30),     // Very short lease
        RenewalInterval = TimeSpan.FromSeconds(10),   // Very frequent renewal
        RetryInterval = TimeSpan.FromSeconds(3),      // Immediate retry
        OperationTimeout = TimeSpan.FromSeconds(5),   // Quick timeout
        AutoStart = true
    };

    /// <summary>
    /// Configuration optimized for resource-constrained environments
    /// </summary>
    public static LeaderElectionOptions ForResourceConstrained() => new()
    {
        LeaseDuration = TimeSpan.FromMinutes(20),     // Long lease to reduce operations
        RenewalInterval = TimeSpan.FromMinutes(6),    // Infrequent renewal
        RetryInterval = TimeSpan.FromMinutes(3),      // Slow retry
        OperationTimeout = TimeSpan.FromMinutes(1),   // Reasonable timeout
        AutoStart = true
    };
}
```

### Connection Pooling and Resource Management

```csharp
public class OptimizedLeaderElectionServiceProvider : ILeaderElectionServiceProvider
{
    private readonly IConnectionPool _connectionPool;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, ILeaderElectionService> _services;

    public OptimizedLeaderElectionServiceProvider(
        IConnectionPool connectionPool,
        ILogger logger)
    {
        _connectionPool = connectionPool;
        _logger = logger;
        _services = new ConcurrentDictionary<string, ILeaderElectionService>();
    }

    public ILeaderElectionService GetService(string electionName, string participantId)
    {
        var key = $"{electionName}:{participantId}";
        
        return _services.GetOrAdd(key, _ =>
        {
            var connection = _connectionPool.GetConnection();
            var leaseStore = new DatabaseLeaseStore(connection, _logger);
            
            return new InProcessLeaderElectionService(
                leaseStore,
                electionName,
                participantId,
                new LeaderElectionOptions
                {
                    LeaseDuration = TimeSpan.FromMinutes(5),
                    RenewalInterval = TimeSpan.FromMinutes(1),
                    RetryInterval = TimeSpan.FromSeconds(30),
                    AutoStart = true
                });
        });
    }

    public async ValueTask DisposeAsync()
    {
        var disposeTasks = _services.Values.Select(service => service.DisposeAsync().AsTask());
        await Task.WhenAll(disposeTasks);
        
        _services.Clear();
        _connectionPool.Dispose();
    }
}
```

## Testing and Simulation

### Redis Integration Testing

#### Docker-based Integration Tests
```csharp
public class RedisLeaderElectionIntegrationTests : IDisposable
{
    private readonly IContainer _redisContainer;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDataSerializer _serializer;

    public RedisLeaderElectionIntegrationTests()
    {
        // Start Redis container for testing
        _redisContainer = new ContainerBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();

        _redisContainer.StartAsync().Wait();

        var connectionString = $"localhost:{_redisContainer.GetMappedPublicPort(6379)}";
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _serializer = new ReflectionJsonDataSerializer();
    }

    [Fact]
    public async Task MultipleServices_ShouldElectSingleLeader()
    {
        // Create multiple services
        var services = new List<RedisLeaderElectionService>();
        for (int i = 0; i < 5; i++)
        {
            var service = new RedisLeaderElectionService(
                _redis,
                _serializer,
                "integration-test",
                $"participant-{i}",
                new RedisLeaderElectionOptions
                {
                    LeaseDuration = TimeSpan.FromSeconds(10),
                    RenewalInterval = TimeSpan.FromSeconds(3),
                    RetryInterval = TimeSpan.FromSeconds(1),
                    AutoStart = true
                });
            services.Add(service);
        }

        try
        {
            // Start all services
            await Task.WhenAll(services.Select(s => s.StartAsync()));

            // Wait for leadership election
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Verify exactly one leader
            var leaders = services.Where(s => s.IsLeader).ToList();
            Assert.Single(leaders);

            // Stop current leader and verify failover
            var currentLeader = leaders[0];
            await currentLeader.StopAsync();

            // Wait for new leader election
            await Task.Delay(TimeSpan.FromSeconds(5));

            var newLeaders = services.Where(s => s.IsLeader).ToList();
            Assert.Single(newLeaders);
            Assert.NotEqual(currentLeader.ParticipantId, newLeaders[0].ParticipantId);
        }
        finally
        {
            foreach (var service in services)
            {
                await service.StopAsync();
                await service.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task RedisFailover_ShouldHandleGracefully()
    {
        var service = new RedisLeaderElectionService(
            _redis,
            _serializer,
            "failover-test",
            "test-participant");

        await service.StartAsync();
        await service.TryAcquireLeadershipAsync();
        
        Assert.True(service.IsLeader);

        // Simulate Redis failure by stopping container
        await _redisContainer.StopAsync();

        // Wait and verify service handles failure
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        // Service should detect it's no longer leader
        Assert.False(service.IsLeader);

        // Restart Redis
        await _redisContainer.StartAsync();
        
        // Service should be able to re-acquire leadership
        await Task.Delay(TimeSpan.FromSeconds(5));
        var canReacquire = await service.TryAcquireLeadershipAsync();
        Assert.True(canReacquire);
    }

    public void Dispose()
    {
        _redis?.Dispose();
        _redisContainer?.DisposeAsync().AsTask().Wait();
    }
}
```

#### Redis Performance Testing
```csharp
public class RedisLeaderElectionPerformanceTests
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDataSerializer _serializer;

    public RedisLeaderElectionPerformanceTests()
    {
        _redis = ConnectionMultiplexer.Connect("localhost:6379");
        _serializer = new ReflectionJsonDataSerializer();
    }

    [Fact]
    public async Task AcquisitionPerformance_ShouldMeetSLA()
    {
        var service = new RedisLeaderElectionService(
            _redis,
            _serializer,
            "perf-test",
            "participant-1");

        var stopwatch = Stopwatch.StartNew();
        const int iterations = 100;

        for (int i = 0; i < iterations; i++)
        {
            await service.TryAcquireLeadershipAsync();
            await service.ReleaseLeadershipAsync();
        }

        stopwatch.Stop();
        
        var avgTime = stopwatch.ElapsedMilliseconds / iterations;
        
        // Assert average acquisition time is under 100ms
        Assert.True(avgTime < 100, $"Average acquisition time {avgTime}ms exceeded 100ms SLA");
    }

    [Fact]
    public async Task ConcurrentAcquisition_ShouldBeThreadSafe()
    {
        const int concurrentTasks = 20;
        var services = new List<RedisLeaderElectionService>();
        var successfulAcquisitions = 0;

        for (int i = 0; i < concurrentTasks; i++)
        {
            var service = new RedisLeaderElectionService(
                _redis,
                _serializer,
                "concurrent-test",
                $"participant-{i}");
            services.Add(service);
        }

        var tasks = services.Select(async service =>
        {
            if (await service.TryAcquireLeadershipAsync())
            {
                Interlocked.Increment(ref successfulAcquisitions);
            }
        });

        await Task.WhenAll(tasks);

        // Only one service should have acquired leadership
        Assert.Equal(1, successfulAcquisitions);
        
        // Cleanup
        foreach (var service in services)
        {
            await service.DisposeAsync();
        }
    }
}
```

### Unit Testing with Mock Lease Store

```csharp
public class MockLeaseStore : ILeaseStore
{
    private readonly Dictionary<string, LeaderInfo> _leases = new();
    private readonly object _lock = new();

    public Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_leases.ContainsKey(electionName))
            {
                var existing = _leases[electionName];
                if (existing.IsValid)
                {
                    return Task.FromResult<LeaderInfo?>(null);
                }
            }

            var lease = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(leaseDuration),
                Metadata = metadata
            };

            _leases[electionName] = lease;
            return Task.FromResult<LeaderInfo?>(lease);
        }
    }

    // Implement other methods...
}

// Test usage
[Test]
public async Task LeaderElection_ShouldAcquireLeadership()
{
    // Arrange
    var mockStore = new MockLeaseStore();
    var service = new InProcessLeaderElectionService(
        mockStore,
        "test-election",
        "test-participant");

    // Act
    var result = await service.TryAcquireLeadershipAsync();

    // Assert
    Assert.IsTrue(result);
    Assert.IsTrue(service.IsLeader);
}
```

### Load Testing

```csharp
public class LeaderElectionLoadTest
{
    public async Task RunLoadTest(int participantCount, TimeSpan duration)
    {
        var participants = new List<ILeaderElectionService>();
        var leadershipChanges = new ConcurrentBag<LeadershipChangedEventArgs>();

        try
        {
            // Create participants
            for (int i = 0; i < participantCount; i++)
            {
                var participant = new InProcessLeaderElectionService(
                    "load-test",
                    $"participant-{i}",
                    new LeaderElectionOptions
                    {
                        LeaseDuration = TimeSpan.FromSeconds(30),
                        RenewalInterval = TimeSpan.FromSeconds(10),
                        RetryInterval = TimeSpan.FromSeconds(3),
                        AutoStart = true
                    });

                participant.LeadershipChanged += (sender, args) =>
                {
                    leadershipChanges.Add(args);
                };

                participants.Add(participant);
            }

            // Start all participants
            await Task.WhenAll(participants.Select(p => p.StartAsync()));

            // Run test for specified duration
            await Task.Delay(duration);

            // Analyze results
            var leadershipGainedCount = leadershipChanges.Count(e => e.LeadershipGained);
            var leadershipLostCount = leadershipChanges.Count(e => e.LeadershipLost);
            var currentLeaderCount = participants.Count(p => p.IsLeader);

            Console.WriteLine($"Load test results:");
            Console.WriteLine($"  Participants: {participantCount}");
            Console.WriteLine($"  Duration: {duration}");
            Console.WriteLine($"  Leadership gained events: {leadershipGainedCount}");
            Console.WriteLine($"  Leadership lost events: {leadershipLostCount}");
            Console.WriteLine($"  Current leaders: {currentLeaderCount}");
            Console.WriteLine($"  Expected leaders: 1");

            // Verify exactly one leader
            Assert.AreEqual(1, currentLeaderCount, "Exactly one leader should exist");
        }
        finally
        {
            // Clean up
            await Task.WhenAll(participants.Select(async p =>
            {
                await p.StopAsync();
                await p.DisposeAsync();
            }));
        }
    }
}
```

## Best Practices for Production

### 1. Monitoring and Alerting

```csharp
public class ProductionLeaderElectionMonitor
{
    private readonly ILeaderElectionService _service;
    private readonly IMetricsCollector _metrics;
    private readonly IAlertManager _alerts;

    public ProductionLeaderElectionMonitor(
        ILeaderElectionService service,
        IMetricsCollector metrics,
        IAlertManager alerts)
    {
        _service = service;
        _metrics = metrics;
        _alerts = alerts;

        _service.LeadershipChanged += OnLeadershipChanged;
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            _metrics.Gauge("leader_election.leadership_duration_start", DateTime.UtcNow.Ticks);
            _alerts.SendInfo($"Leadership acquired by {_service.ParticipantId}");
        }
        else if (args.LeadershipLost)
        {
            var durationTicks = DateTime.UtcNow.Ticks - _metrics.GetGaugeValue("leader_election.leadership_duration_start");
            var duration = TimeSpan.FromTicks(durationTicks);
            
            _metrics.Histogram("leader_election.leadership_duration", duration.TotalSeconds);
            
            if (duration < TimeSpan.FromMinutes(1))
            {
                _alerts.SendWarning($"Short leadership duration: {duration.TotalSeconds}s");
            }
        }
    }
}
```

### 2. Graceful Shutdown

```csharp
public class GracefulShutdownService : IHostedService
{
    private readonly ILeaderElectionService _leaderService;
    private readonly IApplicationLifetime _lifetime;

    public GracefulShutdownService(
        ILeaderElectionService leaderService,
        IApplicationLifetime lifetime)
    {
        _leaderService = leaderService;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _leaderService.StartAsync(cancellationToken);
        
        // Register for shutdown
        _lifetime.ApplicationStopping.Register(OnShutdown);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _leaderService.StopAsync(cancellationToken);
        await _leaderService.DisposeAsync();
    }

    private async void OnShutdown()
    {
        if (_leaderService.IsLeader)
        {
            Console.WriteLine("Gracefully releasing leadership before shutdown...");
            await _leaderService.ReleaseLeadershipAsync();
        }
    }
}
```

### 3. Circuit Breaker Pattern

```csharp
public class CircuitBreakerLeaderElectionService : ILeaderElectionService
{
    private readonly ILeaderElectionService _inner;
    private readonly ICircuitBreaker _circuitBreaker;

    public CircuitBreakerLeaderElectionService(
        ILeaderElectionService inner,
        ICircuitBreaker circuitBreaker)
    {
        _inner = inner;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _inner.TryAcquireLeadershipAsync(cancellationToken);
        });
    }

    // Implement other methods with circuit breaker protection...
}
```

This advanced configuration guide provides you with the tools and knowledge to fine-tune leader election for your specific requirements, implement custom lease stores, and ensure robust production deployments.