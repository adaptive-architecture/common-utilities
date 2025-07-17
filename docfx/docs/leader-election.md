# Leader Election Utilities

The Common.Utilities package provides robust leader election capabilities for distributed systems, allowing multiple service instances to coordinate and elect a single leader for performing critical operations.

## Overview

Leader election is a fundamental pattern in distributed systems where multiple service instances need to coordinate to ensure that only one instance performs a particular task at any given time. This is essential for:

- **Singleton operations**: Ensuring only one instance processes scheduled jobs
- **Resource management**: Preventing duplicate work and resource conflicts
- **Data consistency**: Maintaining consistent state across distributed instances
- **Failover scenarios**: Automatically switching leadership when the current leader fails

## Key Components

### Core Interfaces

- **`ILeaderElectionService`**: Main interface for leader election operations
- **`ILeaderElectionServiceProvider`**: Factory for creating leader election services
- **`ILeaseStore`**: Abstraction for lease storage and coordination
- **`LeaderElectionOptions`**: Configuration options for leader election behavior

### Key Classes

- **`LeaderInfo`**: Immutable record representing leader information
- **`LeadershipChangedEventArgs`**: Event arguments for leadership state changes
- **`InProcessLeaderElectionService`**: In-process implementation for single-application scenarios
- **`RedisLeaderElectionService`**: Redis-based implementation for distributed scenarios
- **`RedisLeaseStore`**: Redis-backed lease storage using atomic operations
- **`RedisLeaderElectionOptions`**: Redis-specific configuration options

## Basic Usage

### Simple Leader Election

```csharp
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

// Create a leader election service
await using var leaderService = new InProcessLeaderElectionService(
    electionName: "my-election",
    participantId: "instance-1");

// Subscribe to leadership changes
leaderService.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine($"I am now the leader! Current leader: {args.CurrentLeader?.ParticipantId}");
        // Start performing leader-only operations
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("I lost leadership");
        // Stop performing leader-only operations
    }
};

// Try to acquire leadership
bool isLeader = await leaderService.TryAcquireLeadershipAsync();
if (isLeader)
{
    Console.WriteLine("Successfully acquired leadership");
}
```

### Automatic Leadership Management

```csharp
var options = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(2),      // How long leadership lasts
    RenewalInterval = TimeSpan.FromSeconds(30),   // How often to renew leadership
    RetryInterval = TimeSpan.FromSeconds(15),     // How often to retry when not leader
    AutoStart = true                              // Automatically start election loop
};

await using var leaderService = new InProcessLeaderElectionService(
    electionName: "background-processor",
    participantId: Environment.MachineName,
    options: options);

// Subscribe to events
leaderService.LeadershipChanged += async (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine("Starting background processing...");
        await StartBackgroundProcessingAsync();
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("Stopping background processing...");
        await StopBackgroundProcessingAsync();
    }
};

// Start the election process
await leaderService.StartAsync();

// Keep the application running
Console.WriteLine("Press any key to stop...");
Console.ReadKey();

// Stop the election process
await leaderService.StopAsync();
```

## AutoStart vs Manual Control

The `AutoStart` option in `LeaderElectionOptions` controls whether the service automatically manages leadership through a background election loop.

### When to Use AutoStart = true (Default)

**AutoStart = true** is ideal for most scenarios where you want continuous leadership management:

```csharp
var options = new LeaderElectionOptions
{
    AutoStart = true,  // Automatically manage leadership
    LeaseDuration = TimeSpan.FromMinutes(5),
    RenewalInterval = TimeSpan.FromMinutes(1)
};

await using var service = new InProcessLeaderElectionService(
    "background-jobs", 
    "worker-01", 
    options);

// Just start the service - it will handle everything automatically
await service.StartAsync();

// The service will:
// - Continuously try to acquire leadership if not leader
// - Automatically renew leadership if currently leader
// - Handle failures and retry automatically
```

**Use AutoStart = true for:**
- Background services that should always try to be active
- Long-running processes that need continuous coordination
- Services where you want "set it and forget it" behavior
- Scenarios where leadership changes are handled purely through events

### When to Use AutoStart = false

**AutoStart = false** gives you complete control over when leadership operations occur:

```csharp
var options = new LeaderElectionOptions
{
    AutoStart = false,  // Manual control
    LeaseDuration = TimeSpan.FromMinutes(10)
};

await using var service = new InProcessLeaderElectionService(
    "manual-election", 
    "instance-01", 
    options);

// Manual leadership acquisition
bool becameLeader = await service.TryAcquireLeadershipAsync();
if (becameLeader)
{
    try
    {
        // Perform leader-only operations
        await ProcessCriticalTaskAsync();
    }
    finally
    {
        // Release leadership when done
        await service.ReleaseLeadershipAsync();
    }
}
```

**Use AutoStart = false for:**
- **Task-based coordination**: When you need leadership for specific operations only
- **Resource-intensive operations**: When continuous leadership monitoring is too expensive
- **Custom retry logic**: When you want to implement your own retry and timing logic
- **Batch processing**: When leadership is needed only during specific processing windows
- **Integration scenarios**: When leader election is part of a larger orchestration system
- **Performance-critical applications**: When you need fine-grained control over when coordination occurs

### Hybrid Approach

You can also start with manual control and then enable automatic management:

```csharp
var options = new LeaderElectionOptions { AutoStart = false };
await using var service = new InProcessLeaderElectionService("hybrid", "instance-01", options);

// First, manually acquire leadership for initial setup
if (await service.TryAcquireLeadershipAsync())
{
    await PerformInitialSetupAsync();
}

// Now enable automatic management for ongoing operations
await service.StartAsync();  // Even with AutoStart=false, this enables monitoring
```

## Redis-Based Distributed Leader Election

For distributed scenarios where multiple applications or services across different machines need to coordinate, the Redis-based implementation provides enterprise-grade leader election capabilities.

### Redis Installation and Setup

```xml
<!-- Add the Redis package -->
<PackageReference Include="AdaptArch.Common.Utilities.Redis" Version="1.0.0" />
```

### Basic Redis Leader Election

```csharp
using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using StackExchange.Redis;

// Configure Redis connection
var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
var serializer = new ReflectionJsonDataSerializer();

// Create Redis-based leader election service
await using var leaderService = new RedisLeaderElectionService(
    connectionMultiplexer: connectionMultiplexer,
    serializer: serializer,
    electionName: "distributed-election",
    participantId: $"{Environment.MachineName}-{Environment.ProcessId}");

// Subscribe to leadership changes
leaderService.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine("🎉 Became distributed leader!");
        var leader = leaderService.CurrentLeader;
        Console.WriteLine($"Leader: {leader?.ParticipantId} on {leader?.Metadata?["hostname"]}");
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("😔 Lost distributed leadership");
    }
};

// Try to acquire leadership across the distributed system
bool isLeader = await leaderService.TryAcquireLeadershipAsync();
if (isLeader)
{
    Console.WriteLine("Successfully acquired distributed leadership!");
}
```

### Redis Leader Election with Options

```csharp
var connectionMultiplexer = ConnectionMultiplexer.Connect("redis-cluster.example.com:6379");
var serializer = new ReflectionJsonDataSerializer();

var options = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(3),      // Distributed lease duration
    RenewalInterval = TimeSpan.FromSeconds(45),   // Frequent renewal for network reliability
    RetryInterval = TimeSpan.FromSeconds(20),     // Network-aware retry interval
    AutoStart = true,
    Metadata = new Dictionary<string, string>
    {
        ["hostname"] = Environment.MachineName,
        ["process_id"] = Environment.ProcessId.ToString(),
        ["version"] = "1.0.0",
        ["datacenter"] = "us-east-1"
    }
};

await using var leaderService = new RedisLeaderElectionService(
    connectionMultiplexer,
    serializer,
    "global-job-processor",
    $"{Environment.MachineName}-{Guid.NewGuid():N}",
    options);

leaderService.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        var leader = leaderService.CurrentLeader;
        Console.WriteLine($"[DISTRIBUTED] Leadership acquired by {leader?.ParticipantId}");
        Console.WriteLine($"[DISTRIBUTED] Leader metadata: {string.Join(", ", leader?.Metadata ?? new Dictionary<string, string>())}");
        
        // Start distributed operations
        StartDistributedProcessing();
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("[DISTRIBUTED] Lost leadership - stopping operations");
        StopDistributedProcessing();
    }
};

// Start automatic distributed leadership management
await leaderService.StartAsync();
```

### Redis Configuration Options

```csharp
var redisOptions = new RedisLeaderElectionOptions
{
    ConnectionMultiplexer = connectionMultiplexer,
    Serializer = serializer,
    Database = 0,              // Redis database index (default: -1)
    KeyPrefix = "my_app"       // Custom key prefix (default: "leader_election")
};

// Validate configuration
redisOptions.Validate(); // Throws if invalid

// Use with service provider
var serviceProvider = new RedisLeaderElectionServiceProvider(
    redisOptions.ConnectionMultiplexer,
    redisOptions.Serializer);

var election = serviceProvider.CreateElection(
    "distributed-service",
    Environment.MachineName);
```

### Using Redis Service Provider

```csharp
// Service provider approach for dependency injection
var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
var serializer = new ReflectionJsonDataSerializer();

var serviceProvider = new RedisLeaderElectionServiceProvider(
    connectionMultiplexer,
    serializer,
    logger: null); // Optional ILogger

// Create multiple elections for different purposes
var jobProcessorElection = serviceProvider.CreateElection(
    "job-processor",
    $"worker-{Environment.MachineName}");

var cacheWarmerElection = serviceProvider.CreateElection(
    "cache-warmer",
    $"cache-{Environment.ProcessId}");

var healthMonitorElection = serviceProvider.CreateElection(
    "health-monitor",
    $"monitor-{Guid.NewGuid():N}");

// Each election is independent
await jobProcessorElection.StartAsync();
await cacheWarmerElection.StartAsync();
await healthMonitorElection.StartAsync();
```

### Redis Key Management

The Redis implementation uses a structured key naming convention:

```
leader_election:lease:{electionName}
```

For example:
- Election "job-processor" → Key: `leader_election:lease:job-processor`
- Election "cache-warmer" → Key: `leader_election:lease:cache-warmer`

With custom key prefix:
```csharp
var options = new RedisLeaderElectionOptions
{
    KeyPrefix = "myapp_elections"
};
// Results in keys like: myapp_elections:lease:job-processor
```

### Redis Atomic Operations

The Redis implementation uses atomic operations for thread-safety across distributed systems:

#### Lease Acquisition
```lua
-- Atomic lease acquisition with SET NX EX
SET leader_election:lease:my-election "{lease_data}" NX EX 180
```

#### Lease Renewal
```lua
-- Atomic lease renewal (only if current holder)
local key = KEYS[1]
local participantId = ARGV[1]
local newLeaseData = ARGV[2]
local ttlSeconds = tonumber(ARGV[3])

local currentLease = redis.call('GET', key)
if currentLease == false then
    return nil
end

local currentParticipantId = cjson.decode(currentLease).ParticipantId
if currentParticipantId ~= participantId then
    return nil
end

redis.call('SET', key, newLeaseData, 'EX', ttlSeconds)
return newLeaseData
```

#### Lease Release
```lua
-- Atomic lease release (only if current holder)
local key = KEYS[1]
local participantId = ARGV[1]

local currentLease = redis.call('GET', key)
if currentLease == false then
    return 0
end

local currentParticipantId = cjson.decode(currentLease).ParticipantId
if currentParticipantId == participantId then
    return redis.call('DEL', key)
end

return 0
```

### Distributed Patterns with Redis

#### Multi-Region Leader Election

```csharp
public class MultiRegionJobProcessor
{
    private readonly RedisLeaderElectionService _globalLeader;
    private readonly RedisLeaderElectionService _regionLeader;
    private volatile bool _isGlobalLeader = false;
    private volatile bool _isRegionLeader = false;

    public MultiRegionJobProcessor(IConnectionMultiplexer redis, string region)
    {
        var serializer = new ReflectionJsonDataSerializer();
        var metadata = new Dictionary<string, string>
        {
            ["region"] = region,
            ["hostname"] = Environment.MachineName,
            ["start_time"] = DateTime.UtcNow.ToString("O")
        };

        var options = new LeaderElectionOptions
        {
            AutoStart = true,
            LeaseDuration = TimeSpan.FromMinutes(2),
            RenewalInterval = TimeSpan.FromSeconds(30),
            Metadata = metadata
        };

        // Global leader across all regions
        _globalLeader = new RedisLeaderElectionService(
            redis, serializer, "global-job-processor", 
            $"{region}-{Environment.MachineName}", options);

        // Regional leader within this region
        _regionLeader = new RedisLeaderElectionService(
            redis, serializer, $"regional-job-processor-{region}", 
            Environment.MachineName, options);

        _globalLeader.LeadershipChanged += OnGlobalLeadershipChanged;
        _regionLeader.LeadershipChanged += OnRegionalLeadershipChanged;
    }

    public async Task StartAsync()
    {
        await _globalLeader.StartAsync();
        await _regionLeader.StartAsync();
    }

    private void OnGlobalLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        _isGlobalLeader = args.IsLeader;
        
        if (args.LeadershipGained)
        {
            Console.WriteLine("[GLOBAL] Became global leader - managing cross-region coordination");
            StartGlobalCoordination();
        }
        else if (args.LeadershipLost)
        {
            Console.WriteLine("[GLOBAL] Lost global leadership");
            StopGlobalCoordination();
        }
    }

    private void OnRegionalLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        _isRegionLeader = args.IsLeader;
        
        if (args.LeadershipGained)
        {
            Console.WriteLine("[REGIONAL] Became regional leader - managing local jobs");
            StartRegionalProcessing();
        }
        else if (args.LeadershipLost)
        {
            Console.WriteLine("[REGIONAL] Lost regional leadership");
            StopRegionalProcessing();
        }
    }

    private void StartGlobalCoordination()
    {
        // Global leader responsibilities:
        // - Cross-region job distribution
        // - Global health monitoring
        // - Inter-region communication
    }

    private void StartRegionalProcessing()
    {
        // Regional leader responsibilities:
        // - Process jobs for this region
        // - Regional health checks
        // - Local resource management
    }
}
```

#### Failover-Aware Background Service

```csharp
public class DistributedBackgroundService : BackgroundService
{
    private readonly RedisLeaderElectionService _leaderService;
    private readonly IJobProcessor _jobProcessor;
    private readonly IConnectionMultiplexer _redis;
    private volatile bool _isLeader = false;

    public DistributedBackgroundService(
        IConnectionMultiplexer redis,
        IJobProcessor jobProcessor,
        ILogger<DistributedBackgroundService> logger)
    {
        _redis = redis;
        _jobProcessor = jobProcessor;

        var serializer = new ReflectionJsonDataSerializer();
        var options = new LeaderElectionOptions
        {
            AutoStart = true,
            LeaseDuration = TimeSpan.FromMinutes(2),
            RenewalInterval = TimeSpan.FromSeconds(30),
            RetryInterval = TimeSpan.FromSeconds(15),
            Metadata = new Dictionary<string, string>
            {
                ["hostname"] = Environment.MachineName,
                ["process_id"] = Environment.ProcessId.ToString(),
                ["start_time"] = DateTime.UtcNow.ToString("O"),
                ["version"] = GetType().Assembly.GetName().Version?.ToString() ?? "unknown"
            }
        };

        _leaderService = new RedisLeaderElectionService(
            redis,
            serializer,
            "distributed-background-service",
            $"{Environment.MachineName}-{Environment.ProcessId}",
            options,
            logger);

        _leaderService.LeadershipChanged += OnLeadershipChanged;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _leaderService.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _leaderService.StopAsync(cancellationToken);
        await _leaderService.DisposeAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isLeader)
            {
                try
                {
                    await ProcessJobsAsLeader(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Error processing jobs: {ex.Message}");
                    // Continue running, leadership will handle failures
                }
            }
            else
            {
                // Follower mode - maybe do health checks or maintenance
                await PerformFollowerTasks(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessJobsAsLeader(CancellationToken cancellationToken)
    {
        // Double-check leadership before processing
        if (!_leaderService.IsLeader) return;

        var jobs = await _jobProcessor.GetPendingJobsAsync(cancellationToken);
        
        foreach (var job in jobs)
        {
            // Check leadership before each job
            if (!_leaderService.IsLeader)
            {
                Console.WriteLine("Lost leadership during job processing - stopping");
                break;
            }

            if (cancellationToken.IsCancellationRequested) break;

            await _jobProcessor.ProcessJobAsync(job, cancellationToken);
        }
    }

    private async Task PerformFollowerTasks(CancellationToken cancellationToken)
    {
        // Tasks that followers can do:
        // - Health monitoring
        // - Cache warming
        // - Cleanup operations
        // - Metrics collection
        
        await _jobProcessor.PerformMaintenanceAsync(cancellationToken);
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        _isLeader = args.IsLeader;

        if (args.LeadershipGained)
        {
            var leader = _leaderService.CurrentLeader;
            Console.WriteLine($"[DISTRIBUTED] Became leader: {leader?.ParticipantId}");
            Console.WriteLine($"[DISTRIBUTED] Leader metadata: {SerializeMetadata(leader?.Metadata)}");
        }
        else if (args.LeadershipLost)
        {
            var currentLeader = _leaderService.CurrentLeader;
            Console.WriteLine($"[DISTRIBUTED] Lost leadership");
            if (currentLeader != null)
            {
                Console.WriteLine($"[DISTRIBUTED] New leader: {currentLeader.ParticipantId}");
            }
        }
    }

    private static string SerializeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata == null || !metadata.Any()) return "none";
        return string.Join(", ", metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}
```

### Redis Connection Management

#### Connection Resilience
```csharp
public class ResilientRedisLeaderElectionService
{
    private RedisLeaderElectionService? _leaderService;
    private readonly ConnectionMultiplexer _redis;
    private readonly string _electionName;
    private readonly string _participantId;
    private readonly LeaderElectionOptions _options;

    public ResilientRedisLeaderElectionService(
        string connectionString,
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null)
    {
        _electionName = electionName;
        _participantId = participantId;
        _options = options ?? new LeaderElectionOptions();

        // Configure Redis with resilience settings
        var configurationOptions = ConfigurationOptions.Parse(connectionString);
        configurationOptions.AbortOnConnectFail = false;
        configurationOptions.ConnectRetry = 3;
        configurationOptions.ConnectTimeout = 10000;
        configurationOptions.SyncTimeout = 5000;
        configurationOptions.AsyncTimeout = 5000;

        _redis = ConnectionMultiplexer.Connect(configurationOptions);
        
        // Monitor connection events
        _redis.ConnectionFailed += OnConnectionFailed;
        _redis.ConnectionRestored += OnConnectionRestored;
    }

    public async Task StartAsync()
    {
        if (_redis.IsConnected)
        {
            await CreateLeaderServiceAsync();
        }
        else
        {
            Console.WriteLine("Redis not connected - waiting for connection...");
        }
    }

    private async Task CreateLeaderServiceAsync()
    {
        var serializer = new ReflectionJsonDataSerializer();
        
        _leaderService = new RedisLeaderElectionService(
            _redis,
            serializer,
            _electionName,
            _participantId,
            _options);

        _leaderService.LeadershipChanged += OnLeadershipChanged;
        await _leaderService.StartAsync();
    }

    private async void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        Console.WriteLine($"Redis connection failed: {e.Exception?.Message}");
        
        if (_leaderService != null)
        {
            await _leaderService.StopAsync();
            await _leaderService.DisposeAsync();
            _leaderService = null;
        }
    }

    private async void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        Console.WriteLine("Redis connection restored - resuming leader election");
        await CreateLeaderServiceAsync();
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        // Handle leadership changes with connection awareness
        if (args.LeadershipGained)
        {
            Console.WriteLine($"[RESILIENT] Gained leadership (Redis connected: {_redis.IsConnected})");
        }
        else if (args.LeadershipLost)
        {
            Console.WriteLine($"[RESILIENT] Lost leadership (Redis connected: {_redis.IsConnected})");
        }
    }
}
```

### Performance Tuning for Redis

#### High-Frequency Elections
```csharp
var highFrequencyOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromSeconds(30),     // Short lease for quick failover
    RenewalInterval = TimeSpan.FromSeconds(10),   // Frequent renewal
    RetryInterval = TimeSpan.FromSeconds(5),      // Quick retry
    OperationTimeout = TimeSpan.FromSeconds(5)    // Short timeout for network ops
};
```

#### Low-Frequency Elections
```csharp
var lowFrequencyOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(10),     // Long lease for stability
    RenewalInterval = TimeSpan.FromMinutes(3),    // Infrequent renewal
    RetryInterval = TimeSpan.FromMinutes(1),      // Slow retry
    OperationTimeout = TimeSpan.FromSeconds(30)   // Longer timeout for reliability
};
```

### Redis vs In-Process Comparison

| Feature | InProcess | Redis |
|---------|-----------|--------|
| **Scope** | Single application | Distributed across machines |
| **Persistence** | Memory only | Persisted in Redis |
| **Network** | No network calls | Network-dependent |
| **Failover Speed** | Instant | Network latency dependent |
| **Scalability** | Single process | Horizontally scalable |
| **Complexity** | Simple | Requires Redis infrastructure |
| **Use Cases** | Multiple threads/services in one app | Multiple applications/machines |

### Redis Monitoring and Diagnostics

```csharp
// Add metadata for monitoring
var diagnosticMetadata = new Dictionary<string, string>
{
    ["hostname"] = Environment.MachineName,
    ["process_id"] = Environment.ProcessId.ToString(),
    ["version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
    ["datacenter"] = "us-east-1",
    ["environment"] = "production",
    ["start_time"] = DateTime.UtcNow.ToString("O")
};

var options = new LeaderElectionOptions
{
    Metadata = diagnosticMetadata,
    LeaseDuration = TimeSpan.FromMinutes(3),
    RenewalInterval = TimeSpan.FromSeconds(45)
};

// Monitor leader changes across the cluster
leaderService.LeadershipChanged += (sender, args) =>
{
    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    
    if (args.LeadershipGained)
    {
        Console.WriteLine($"[{timestamp}] GAINED leadership");
        LogLeaderInfo(leaderService.CurrentLeader, "NEW LEADER");
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine($"[{timestamp}] LOST leadership");
        LogLeaderInfo(leaderService.CurrentLeader, "CURRENT LEADER");
    }
};

static void LogLeaderInfo(LeaderInfo? leader, string prefix)
{
    if (leader == null)
    {
        Console.WriteLine($"{prefix}: None");
        return;
    }

    Console.WriteLine($"{prefix}: {leader.ParticipantId}");
    Console.WriteLine($"  Acquired: {leader.AcquiredAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine($"  Expires: {leader.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine($"  Valid: {leader.IsValid}");
    Console.WriteLine($"  Time to expiry: {leader.TimeToExpiry.TotalSeconds:F1}s");
    
    if (leader.Metadata != null && leader.Metadata.Any())
    {
        Console.WriteLine("  Metadata:");
        foreach (var kvp in leader.Metadata)
        {
            Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
        }
    }
}
```

## Configuration Options

### LeaderElectionOptions Properties

```csharp
public class LeaderElectionOptions
{
    /// <summary>
    /// How long a lease lasts before it expires (default: 5 minutes)
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How often the leader renews its lease (default: 1 minute)
    /// </summary>
    public TimeSpan RenewalInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How often non-leaders retry to acquire leadership (default: 30 seconds)
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time to wait for lease operations (default: 30 seconds)
    /// </summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to automatically start the election loop (default: true)
    /// </summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Custom metadata to attach to the lease
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
}
```

### Timing Configuration Examples

```csharp
// High-frequency leadership for time-sensitive operations
var highFrequencyOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromSeconds(30),
    RenewalInterval = TimeSpan.FromSeconds(10),
    RetryInterval = TimeSpan.FromSeconds(5),
    OperationTimeout = TimeSpan.FromSeconds(10)
};

// Low-frequency leadership for background tasks
var lowFrequencyOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(10),
    RenewalInterval = TimeSpan.FromMinutes(3),
    RetryInterval = TimeSpan.FromMinutes(1),
    OperationTimeout = TimeSpan.FromMinutes(1)
};

// Custom metadata for leader identification
var metadataOptions = new LeaderElectionOptions
{
    Metadata = new Dictionary<string, string>
    {
        ["Version"] = "1.0.0",
        ["Environment"] = "Production",
        ["Region"] = "us-east-1"
    }
};
```

## Event Handling

### LeadershipChanged Event

```csharp
service.LeadershipChanged += (sender, args) =>
{
    // Access leadership state
    bool isLeader = args.IsLeader;
    bool gainedLeadership = args.LeadershipGained;
    bool lostLeadership = args.LeadershipLost;
    
    // Access leader information
    LeaderInfo? currentLeader = args.CurrentLeader;
    LeaderInfo? previousLeader = args.PreviousLeader;
    
    if (currentLeader != null)
    {
        Console.WriteLine($"Current leader: {currentLeader.ParticipantId}");
        Console.WriteLine($"Leader since: {currentLeader.AcquiredAt}");
        Console.WriteLine($"Lease expires: {currentLeader.ExpiresAt}");
        Console.WriteLine($"Is valid: {currentLeader.IsValid}");
        Console.WriteLine($"Time to expiry: {currentLeader.TimeToExpiry}");
    }
};
```

> **Important Note**: The `LeadershipChangedEventArgs` does not always contain complete information about the current and previous leader. In some scenarios (such as when lease operations fail or during error conditions), the `CurrentLeader` or `PreviousLeader` properties may be `null` even when leadership changes occur. Always check for null values and use the service's `CurrentLeader` property for the most reliable leader information.

```csharp
// Recommended approach - always check for null and use service properties
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine("I gained leadership!");
        // Use service.CurrentLeader for reliable leader info
        var currentLeader = service.CurrentLeader;
        if (currentLeader != null)
        {
            Console.WriteLine($"Leader: {currentLeader.ParticipantId}");
        }
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("I lost leadership");
        // args.CurrentLeader might be null, so check service state
        var isStillLeader = service.IsLeader;
        var actualCurrentLeader = service.CurrentLeader;
    }
};
```

### Leadership State Properties

```csharp
// Current leadership status
bool isCurrentlyLeader = service.IsLeader;

// Information about the current leader (may be null)
LeaderInfo? currentLeader = service.CurrentLeader;

// Service identification
string electionName = service.ElectionName;
string participantId = service.ParticipantId;
```

## Error Handling and Resilience

The leader election service is designed to be resilient to failures and handles various error conditions gracefully:

### Automatic Error Recovery

```csharp
// The service automatically handles:
// - Network failures during lease operations
// - Temporary unavailability of the lease store
// - Timeout exceptions during coordination
// - Lease expiration and renewal failures

service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipLost)
    {
        // This could be due to:
        // - Network partition
        // - Lease store failure
        // - Renewal timeout
        // - Another instance taking over
        
        // Gracefully handle the transition
        await CleanupLeaderOperationsAsync();
    }
};
```

### Manual Error Handling

```csharp
try
{
    bool acquired = await service.TryAcquireLeadershipAsync();
    if (!acquired)
    {
        Console.WriteLine("Failed to acquire leadership - another instance is leader");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Leadership acquisition was cancelled");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error during leadership acquisition: {ex.Message}");
}
```

## Real-World Examples

### Background Job Processor

```csharp
public class BackgroundJobProcessor : IHostedService
{
    private readonly ILeaderElectionService _leaderService;
    private readonly IJobQueue _jobQueue;
    private Timer? _processingTimer;

    public BackgroundJobProcessor(IJobQueue jobQueue)
    {
        _jobQueue = jobQueue;
        _leaderService = new InProcessLeaderElectionService(
            "job-processor",
            Environment.MachineName,
            new LeaderElectionOptions
            {
                LeaseDuration = TimeSpan.FromMinutes(3),
                RenewalInterval = TimeSpan.FromMinutes(1),
                AutoStart = true
            });

        _leaderService.LeadershipChanged += OnLeadershipChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _leaderService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _processingTimer?.Dispose();
        await _leaderService.StopAsync(cancellationToken);
        await _leaderService.DisposeAsync();
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            // Start processing jobs as the leader
            _processingTimer = new Timer(ProcessJobs, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        else if (args.LeadershipLost)
        {
            // Stop processing jobs
            _processingTimer?.Dispose();
            _processingTimer = null;
        }
    }

    private async void ProcessJobs(object? state)
    {
        if (!_leaderService.IsLeader) return;

        try
        {
            var jobs = await _jobQueue.GetPendingJobsAsync();
            foreach (var job in jobs)
            {
                if (!_leaderService.IsLeader) break; // Lost leadership
                
                await ProcessJobAsync(job);
            }
        }
        catch (Exception ex)
        {
            // Log error but continue processing
            Console.WriteLine($"Error processing jobs: {ex.Message}");
        }
    }

    private async Task ProcessJobAsync(IJob job)
    {
        // Process individual job
        await job.ExecuteAsync();
    }
}
```

### Distributed Cache Warmer

```csharp
public class DistributedCacheWarmer
{
    private readonly ILeaderElectionService _leaderService;
    private readonly IDistributedCache _cache;
    private readonly IDataService _dataService;
    private CancellationTokenSource? _warmupCancellation;

    public DistributedCacheWarmer(IDistributedCache cache, IDataService dataService)
    {
        _cache = cache;
        _dataService = dataService;
        
        _leaderService = new InProcessLeaderElectionService(
            "cache-warmer",
            $"{Environment.MachineName}-{Environment.ProcessId}",
            new LeaderElectionOptions
            {
                LeaseDuration = TimeSpan.FromMinutes(5),
                RenewalInterval = TimeSpan.FromMinutes(1),
                AutoStart = false // Manual control for this scenario
            });
    }

    public async Task WarmCacheAsync(CancellationToken cancellationToken = default)
    {
        // Try to acquire leadership for cache warming
        bool becameLeader = await _leaderService.TryAcquireLeadershipAsync(cancellationToken);
        
        if (!becameLeader)
        {
            Console.WriteLine("Another instance is warming the cache");
            return;
        }

        try
        {
            Console.WriteLine("Starting cache warm-up as leader");
            _warmupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Perform cache warming operations
            await WarmupCriticalDataAsync(_warmupCancellation.Token);
            await WarmupUserDataAsync(_warmupCancellation.Token);
            await WarmupConfigurationAsync(_warmupCancellation.Token);
            
            Console.WriteLine("Cache warm-up completed successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Cache warm-up was cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cache warm-up: {ex.Message}");
        }
        finally
        {
            // Release leadership when done
            await _leaderService.ReleaseLeadershipAsync();
            _warmupCancellation?.Dispose();
        }
    }

    private async Task WarmupCriticalDataAsync(CancellationToken cancellationToken)
    {
        var criticalData = await _dataService.GetCriticalDataAsync(cancellationToken);
        foreach (var item in criticalData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _cache.SetAsync($"critical:{item.Id}", item.ToJson(), cancellationToken);
        }
    }

    private async Task WarmupUserDataAsync(CancellationToken cancellationToken)
    {
        var activeUsers = await _dataService.GetActiveUsersAsync(cancellationToken);
        foreach (var user in activeUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _cache.SetAsync($"user:{user.Id}", user.ToJson(), cancellationToken);
        }
    }

    private async Task WarmupConfigurationAsync(CancellationToken cancellationToken)
    {
        var config = await _dataService.GetConfigurationAsync(cancellationToken);
        await _cache.SetAsync("app:configuration", config.ToJson(), cancellationToken);
    }
}
```

## Performance Considerations

### Timing Trade-offs

```csharp
// Fast failover (higher network traffic)
var fastFailoverOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromSeconds(30),     // Short lease
    RenewalInterval = TimeSpan.FromSeconds(10),   // Frequent renewal
    RetryInterval = TimeSpan.FromSeconds(5)       // Quick retry
};

// Slow failover (lower network traffic)
var slowFailoverOptions = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(10),     // Long lease
    RenewalInterval = TimeSpan.FromMinutes(3),    // Infrequent renewal
    RetryInterval = TimeSpan.FromMinutes(1)       // Slow retry
};
```

### Resource Management

```csharp
// Proper disposal
await using var service = new InProcessLeaderElectionService(
    "my-election", 
    "my-participant");

// Service automatically releases resources on disposal
```

## Best Practices

1. **Use meaningful election names**: Choose descriptive names that clearly indicate the purpose
2. **Set appropriate timeouts**: Balance between quick failover and network efficiency
3. **Handle leadership transitions gracefully**: Always clean up resources when losing leadership
4. **Monitor leadership status**: Use events to track leadership changes
5. **Use service properties for reliable information**: Don't rely solely on event arguments for leader information, as they may be incomplete during error conditions
6. **Consider AutoStart settings**: Choose based on your specific use case requirements
7. **Test failover scenarios**: Ensure your application handles leadership transitions correctly
8. **Use unique participant IDs**: Avoid conflicts by using machine names or process IDs
9. **Handle exceptions properly**: Implement robust error handling for network issues
10. **Implement defensive event handling**: Always check for null values in event handlers and use try-catch blocks
11. **Log leadership events**: Track leadership changes for debugging and monitoring
12. **Release resources promptly**: Always dispose of services when shutting down

The leader election utilities provide a solid foundation for building distributed systems that require coordination and single-leader semantics, with built-in resilience and flexible configuration options.