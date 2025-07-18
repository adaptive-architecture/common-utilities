# PostgreSQL Leader Election

The PostgreSQL leader election implementation provides enterprise-grade distributed leader election using PostgreSQL as the coordination backend. This implementation offers strong consistency, persistence, and reliability for mission-critical distributed systems.

## Overview

PostgreSQL leader election uses PostgreSQL's atomic operations and ACID properties to ensure safe, consistent leader coordination across distributed systems. Unlike Redis-based solutions, PostgreSQL provides:

- **Strong consistency**: ACID compliance ensures reliable coordination
- **Persistence**: Leases survive database restarts and failures
- **Enterprise reliability**: Battle-tested PostgreSQL infrastructure
- **SQL-based debugging**: Easy to inspect and debug lease state
- **Automatic cleanup**: Built-in expired lease cleanup capabilities

## Key Components

### Core Classes

- **`PostgresLeaderElectionService`**: Main service for PostgreSQL-based leader election
- **`PostgresLeaseStore`**: PostgreSQL-backed lease storage with atomic operations
- **`PostgresLeaderElectionServiceProvider`**: Factory for creating PostgreSQL election services

### PostgreSQL Schema

The implementation uses a simple, efficient table structure:

```sql
CREATE TABLE leader_election_leases (
    election_name VARCHAR(255) PRIMARY KEY,
    participant_id VARCHAR(255) NOT NULL,
    acquired_at TIMESTAMP WITH TIME ZONE NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    metadata JSONB
);

CREATE INDEX idx_leader_election_leases_expires_at ON leader_election_leases(expires_at);
```

## Installation

Add the PostgreSQL package to your project:

```xml
<PackageReference Include="AdaptArch.Common.Utilities.Postgres" Version="1.0.0" />
```

## Basic Usage

### Simple PostgreSQL Leader Election

```csharp
using AdaptArch.Common.Utilities.Postgres.LeaderElection;
using AdaptArch.Common.Utilities.Serialization.Implementations;
using Npgsql;

// Create PostgreSQL data source
var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=myapp;Username=postgres;Password=password");
var serializer = new ReflectionStringJsonDataSerializer();

// Create PostgreSQL-based leader election service
await using var leaderService = new PostgresLeaderElectionService(
    dataSource: dataSource,
    serializer: serializer,
    electionName: "background-processor",
    participantId: $"{Environment.MachineName}-{Environment.ProcessId}");

// Subscribe to leadership changes
leaderService.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine("🎉 Became PostgreSQL leader!");
        Console.WriteLine($"Leader: {leaderService.CurrentLeader?.ParticipantId}");
        // Start leader-only operations
        StartBackgroundProcessing();
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("😔 Lost PostgreSQL leadership");
        // Stop leader-only operations
        StopBackgroundProcessing();
    }
};

// Try to acquire leadership
bool isLeader = await leaderService.TryAcquireLeadershipAsync();
if (isLeader)
{
    Console.WriteLine("Successfully acquired PostgreSQL leadership!");
}
```

### Automatic Leadership Management

```csharp
var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=myapp;Username=postgres;Password=password");
var serializer = new ReflectionStringJsonDataSerializer();

var options = LeaderElectionOptions.Create(
    TimeSpan.FromMinutes(3),                     // PostgreSQL lease duration
    new Dictionary<string, string>
    {
        ["hostname"] = Environment.MachineName,
        ["process_id"] = Environment.ProcessId.ToString(),
        ["version"] = "1.0.0"
    });

// The Create method automatically calculates optimal timing:
// - RenewalInterval: 1 minute (3 min / 3)
// - RetryInterval: 30 seconds (3 min / 6)  
// - OperationTimeout: 30 seconds (3 min / 6)
// - EnableContinuousCheck: true (default)

await using var leaderService = new PostgresLeaderElectionService(
    dataSource,
    serializer,
    "distributed-job-processor",
    $"{Environment.MachineName}-{Guid.NewGuid():N}",
    options);

// Subscribe to events
leaderService.LeadershipChanged += async (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine("[POSTGRES] Starting distributed processing...");
        await StartDistributedProcessingAsync();
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("[POSTGRES] Stopping distributed processing...");
        await StopDistributedProcessingAsync();
    }
};

// Start automatic leadership management
await leaderService.StartAsync();

// Keep the application running
Console.WriteLine("Press any key to stop...");
Console.ReadKey();

// Stop the service
await leaderService.StopAsync();
```

## Advanced Configuration

### Simplified Configuration with Create Method

The `LeaderElectionOptions.Create` static method provides the easiest way to create properly configured options for PostgreSQL leader election:

```csharp
// Simple configuration - just specify lease duration
var options = LeaderElectionOptions.Create(
    TimeSpan.FromMinutes(2),                    // Lease duration
    null);                                      // No metadata

// Advanced configuration with metadata
var options = LeaderElectionOptions.Create(
    TimeSpan.FromMinutes(5),                    // Lease duration  
    new Dictionary<string, string>
    {
        ["hostname"] = Environment.MachineName,
        ["process_id"] = Environment.ProcessId.ToString(),
        ["database"] = "production",
        ["version"] = "1.0.0",
        ["datacenter"] = "us-east-1"
    });

// The Create method automatically calculates optimal timing relationships:
// - RenewalInterval: lease duration / 3 (ensures multiple renewal attempts)
// - RetryInterval: lease duration / 6 (allows several retry attempts)
// - OperationTimeout: lease duration / 6 (prevents blocking operations)
```

### PostgreSQL-Specific Timing Recommendations

For PostgreSQL deployments, consider these timing patterns:

```csharp
// High-availability setup (fast failover)
var haOptions = LeaderElectionOptions.Create(
    TimeSpan.FromSeconds(30),                   // 30-second lease
    new Dictionary<string, string> { ["type"] = "high-availability" });
// Results in: 10s renewal, 5s retry, 5s timeout

// Standard production setup
var productionOptions = LeaderElectionOptions.Create(
    TimeSpan.FromMinutes(3),                    // 3-minute lease
    new Dictionary<string, string> { ["type"] = "production" });
// Results in: 1m renewal, 30s retry, 30s timeout

// Low-frequency background processing
var backgroundOptions = LeaderElectionOptions.Create(
    TimeSpan.FromMinutes(15),                   // 15-minute lease
    new Dictionary<string, string> { ["type"] = "background" });
// Results in: 5m renewal, 2.5m retry, 2.5m timeout
```

### Custom Table Names

```csharp
// Use custom table name for isolation
await using var leaderService = new PostgresLeaderElectionService(
    dataSource,
    serializer,
    "my-election",
    "my-participant",
    tableName: "my_app_leader_elections");  // Custom table name

// Ensure table exists with custom name
var leaseStore = new PostgresLeaseStore(dataSource, serializer, "my_app_leader_elections");
await leaseStore.EnsureTableExistsAsync();
```

### Connection Pooling and Performance

```csharp
// Configure connection pooling for high-performance scenarios
var connectionString = "Host=localhost;Database=myapp;Username=postgres;Password=password;" +
                      "Pooling=true;MinPoolSize=5;MaxPoolSize=20;ConnectionIdleLifetime=300";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
// Configure additional settings
dataSourceBuilder.EnableParameterLogging();
dataSourceBuilder.EnableStatisticsCollection();

await using var dataSource = dataSourceBuilder.Build();

// Use with leader election
await using var leaderService = new PostgresLeaderElectionService(
    dataSource,
    serializer,
    "high-performance-election",
    Environment.MachineName);
```

### High Availability Configuration

```csharp
// Configure for high availability with multiple PostgreSQL instances
var primaryConnectionString = "Host=postgres-primary.example.com;Database=myapp;Username=postgres;Password=password";
var readonlyConnectionString = "Host=postgres-replica.example.com;Database=myapp;Username=postgres;Password=password";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(primaryConnectionString);
dataSourceBuilder.UseRandomizedLoadBalancing();
dataSourceBuilder.EnableReadOnlyMode(false); // Ensure writes go to primary

await using var dataSource = dataSourceBuilder.Build();

var options = new LeaderElectionOptions
{
    LeaseDuration = TimeSpan.FromMinutes(2),      // Shorter lease for HA
    RenewalInterval = TimeSpan.FromSeconds(30),   // Frequent renewal
    RetryInterval = TimeSpan.FromSeconds(15),     // Quick retry
    OperationTimeout = TimeSpan.FromSeconds(10),  // Short timeout
    EnableContinuousCheck = true,
    Metadata = new Dictionary<string, string>
    {
        ["datacenter"] = "us-east-1",
        ["availability_zone"] = "us-east-1a",
        ["instance_type"] = "production"
    }
};

await using var leaderService = new PostgresLeaderElectionService(
    dataSource,
    serializer,
    "ha-distributed-service",
    $"{Environment.MachineName}-{Environment.ProcessId}",
    options);
```

## Service Provider Pattern

### Basic Service Provider Usage

```csharp
using AdaptArch.Common.Utilities.Postgres.LeaderElection;

// Create service provider
var dataSource = NpgsqlDataSource.Create("Host=localhost;Database=myapp;Username=postgres;Password=password");
var serializer = new ReflectionStringJsonDataSerializer();

var serviceProvider = new PostgresLeaderElectionServiceProvider(
    dataSource,
    serializer,
    defaultTableName: "leader_elections",
    logger: loggerFactory.CreateLogger<PostgresLeaderElectionServiceProvider>());

// Create multiple independent elections
var jobProcessorElection = serviceProvider.CreateElection(
    "job-processor",
    $"worker-{Environment.MachineName}");

var cacheWarmerElection = serviceProvider.CreateElection(
    "cache-warmer",
    $"cache-{Environment.ProcessId}");

var healthMonitorElection = serviceProvider.CreateElection(
    "health-monitor",
    $"monitor-{Guid.NewGuid():N}");

// Each election is independent and can be managed separately
await Task.WhenAll(
    jobProcessorElection.StartAsync(),
    cacheWarmerElection.StartAsync(),
    healthMonitorElection.StartAsync());
```

### Dependency Injection Integration

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<NpgsqlDataSource>(provider =>
{
    var connectionString = configuration.GetConnectionString("PostgreSQL");
    return NpgsqlDataSource.Create(connectionString);
});

services.AddSingleton<IStringDataSerializer, ReflectionStringJsonDataSerializer>();

services.AddSingleton<PostgresLeaderElectionServiceProvider>(provider =>
{
    var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
    var serializer = provider.GetRequiredService<IStringDataSerializer>();
    var logger = provider.GetRequiredService<ILogger<PostgresLeaderElectionServiceProvider>>();
    
    return new PostgresLeaderElectionServiceProvider(dataSource, serializer, logger: logger);
});

// Use in services
public class MyBackgroundService : BackgroundService
{
    private readonly PostgresLeaderElectionServiceProvider _electionProvider;
    private PostgresLeaderElectionService? _leaderService;
    
    public MyBackgroundService(PostgresLeaderElectionServiceProvider electionProvider)
    {
        _electionProvider = electionProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _leaderService = _electionProvider.CreateElection(
            "background-service",
            $"{Environment.MachineName}-{Environment.ProcessId}");
            
        await _leaderService.StartAsync(stoppingToken);
        
        // Service logic...
    }
}
```

## PostgreSQL-Specific Features

### Atomic Operations

The PostgreSQL implementation uses atomic SQL operations for thread-safe coordination:

#### Lease Acquisition
```sql
-- Atomic lease acquisition with conflict handling
INSERT INTO leader_election_leases (election_name, participant_id, acquired_at, expires_at, metadata)
VALUES (@electionName, @participantId, @acquiredAt, @expiresAt, @metadata)
ON CONFLICT (election_name) DO UPDATE SET
    participant_id = @participantId,
    acquired_at = @acquiredAt,
    expires_at = @expiresAt,
    metadata = @metadata
WHERE leader_election_leases.expires_at < @now
RETURNING participant_id, acquired_at, expires_at, metadata;
```

#### Lease Renewal
```sql
-- Atomic lease renewal (only if current holder)
UPDATE leader_election_leases SET
    acquired_at = @acquiredAt,
    expires_at = @expiresAt,
    metadata = @metadata
WHERE election_name = @electionName
  AND participant_id = @participantId
  AND expires_at > @now
RETURNING participant_id, acquired_at, expires_at, metadata;
```

#### Lease Release
```sql
-- Atomic lease release
DELETE FROM leader_election_leases
WHERE election_name = @electionName
  AND participant_id = @participantId;
```

### Metadata Storage with JSONB

PostgreSQL's JSONB support provides efficient metadata storage:

```csharp
var metadata = new Dictionary<string, string>
{
    ["hostname"] = Environment.MachineName,
    ["process_id"] = Environment.ProcessId.ToString(),
    ["version"] = "1.0.0",
    ["datacenter"] = "us-east-1",
    ["deployment"] = "production",
    ["start_time"] = DateTime.UtcNow.ToString("O")
};

var options = new LeaderElectionOptions { Metadata = metadata };

await using var leaderService = new PostgresLeaderElectionService(
    dataSource, serializer, "metadata-election", "participant-1", options);
```

You can query metadata directly in PostgreSQL:

```sql
-- Find leaders by datacenter
SELECT election_name, participant_id, metadata->>'datacenter' as datacenter
FROM leader_election_leases
WHERE metadata->>'datacenter' = 'us-east-1';

-- Find leaders by version
SELECT election_name, participant_id, metadata->>'version' as version
FROM leader_election_leases
WHERE metadata->>'version' = '1.0.0';
```

### Expired Lease Cleanup

```csharp
// Manual cleanup of expired leases
var leaseStore = new PostgresLeaseStore(dataSource, serializer);
int cleanedUp = await leaseStore.CleanupExpiredLeasesAsync();
Console.WriteLine($"Cleaned up {cleanedUp} expired leases");

// You can also set up automatic cleanup
var cleanupTimer = new Timer(async _ =>
{
    try
    {
        await leaseStore.CleanupExpiredLeasesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during cleanup: {ex.Message}");
    }
}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
```

## Real-World Examples

### Distributed Background Service

```csharp
public class PostgresBackgroundService : BackgroundService
{
    private readonly PostgresLeaderElectionService _leaderService;
    private readonly IJobProcessor _jobProcessor;
    private readonly ILogger<PostgresBackgroundService> _logger;
    private volatile bool _isLeader = false;

    public PostgresBackgroundService(
        NpgsqlDataSource dataSource,
        IStringDataSerializer serializer,
        IJobProcessor jobProcessor,
        ILogger<PostgresBackgroundService> logger)
    {
        _jobProcessor = jobProcessor;
        _logger = logger;

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            LeaseDuration = TimeSpan.FromMinutes(2),
            RenewalInterval = TimeSpan.FromSeconds(30),
            RetryInterval = TimeSpan.FromSeconds(15),
            Metadata = new Dictionary<string, string>
            {
                ["hostname"] = Environment.MachineName,
                ["process_id"] = Environment.ProcessId.ToString(),
                ["start_time"] = DateTime.UtcNow.ToString("O")
            }
        };

        _leaderService = new PostgresLeaderElectionService(
            dataSource,
            serializer,
            "postgres-background-service",
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
                    _logger.LogError(ex, "Error processing jobs as leader");
                }
            }
            else
            {
                // Follower tasks
                await PerformMaintenanceTasks(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessJobsAsLeader(CancellationToken cancellationToken)
    {
        // Double-check leadership before processing
        if (!_leaderService.IsLeader) return;

        _logger.LogInformation("Processing jobs as PostgreSQL leader");
        
        var jobs = await _jobProcessor.GetPendingJobsAsync(cancellationToken);
        
        foreach (var job in jobs)
        {
            // Check leadership before each job
            if (!_leaderService.IsLeader)
            {
                _logger.LogWarning("Lost leadership during job processing");
                break;
            }

            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await _jobProcessor.ProcessJobAsync(job, cancellationToken);
                _logger.LogDebug("Processed job {JobId}", job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", job.Id);
            }
        }
    }

    private async Task PerformMaintenanceTasks(CancellationToken cancellationToken)
    {
        // Tasks that followers can perform
        await _jobProcessor.PerformMaintenanceAsync(cancellationToken);
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        _isLeader = args.IsLeader;

        if (args.LeadershipGained)
        {
            var leader = _leaderService.CurrentLeader;
            _logger.LogInformation("Gained PostgreSQL leadership: {ParticipantId}", leader?.ParticipantId);
        }
        else if (args.LeadershipLost)
        {
            _logger.LogInformation("Lost PostgreSQL leadership");
        }
    }
}
```

### Multi-Database Leader Election

```csharp
public class MultiDatabaseLeaderElectionService
{
    private readonly PostgresLeaderElectionService _primaryElection;
    private readonly PostgresLeaderElectionService _secondaryElection;
    private readonly ILogger _logger;
    
    public MultiDatabaseLeaderElectionService(
        NpgsqlDataSource primaryDataSource,
        NpgsqlDataSource secondaryDataSource,
        IStringDataSerializer serializer,
        ILogger logger)
    {
        _logger = logger;
        
        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            LeaseDuration = TimeSpan.FromMinutes(2),
            RenewalInterval = TimeSpan.FromSeconds(30),
            Metadata = new Dictionary<string, string>
            {
                ["instance"] = Environment.MachineName,
                ["role"] = "multi-db-coordinator"
            }
        };

        // Primary database election
        _primaryElection = new PostgresLeaderElectionService(
            primaryDataSource,
            serializer,
            "primary-db-coordinator",
            $"primary-{Environment.MachineName}",
            options);

        // Secondary database election
        _secondaryElection = new PostgresLeaderElectionService(
            secondaryDataSource,
            serializer,
            "secondary-db-coordinator",
            $"secondary-{Environment.MachineName}",
            options);

        _primaryElection.LeadershipChanged += OnPrimaryLeadershipChanged;
        _secondaryElection.LeadershipChanged += OnSecondaryLeadershipChanged;
    }

    public async Task StartAsync()
    {
        await _primaryElection.StartAsync();
        await _secondaryElection.StartAsync();
    }

    public async Task StopAsync()
    {
        await _primaryElection.StopAsync();
        await _secondaryElection.StopAsync();
        await _primaryElection.DisposeAsync();
        await _secondaryElection.DisposeAsync();
    }

    private void OnPrimaryLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            _logger.LogInformation("Became primary database coordinator");
            StartPrimaryDatabaseTasks();
        }
        else if (args.LeadershipLost)
        {
            _logger.LogInformation("Lost primary database coordination");
            StopPrimaryDatabaseTasks();
        }
    }

    private void OnSecondaryLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            _logger.LogInformation("Became secondary database coordinator");
            StartSecondaryDatabaseTasks();
        }
        else if (args.LeadershipLost)
        {
            _logger.LogInformation("Lost secondary database coordination");
            StopSecondaryDatabaseTasks();
        }
    }

    private void StartPrimaryDatabaseTasks()
    {
        // Coordinate primary database operations
        // - Primary backups
        // - Primary maintenance
        // - Primary monitoring
    }

    private void StartSecondaryDatabaseTasks()
    {
        // Coordinate secondary database operations
        // - Secondary backups
        // - Read replica maintenance
        // - Secondary monitoring
    }

    private void StopPrimaryDatabaseTasks()
    {
        // Stop primary database coordination
    }

    private void StopSecondaryDatabaseTasks()
    {
        // Stop secondary database coordination
    }
}
```

## Performance Optimization

### Connection Pooling

```csharp
// Optimize connection pooling for leader election
var connectionString = "Host=localhost;Database=myapp;Username=postgres;Password=password;" +
                      "Pooling=true;" +
                      "MinPoolSize=2;" +        // Minimum connections
                      "MaxPoolSize=10;" +       // Maximum connections
                      "ConnectionIdleLifetime=600;" + // 10 minutes
                      "ConnectionPruningInterval=10;" + // Prune every 10 seconds
                      "IncludeErrorDetail=true";

var dataSource = NpgsqlDataSource.Create(connectionString);
```

### Batch Operations

```csharp
// For scenarios with multiple elections, use a single data source
var dataSource = NpgsqlDataSource.Create(connectionString);
var serializer = new ReflectionStringJsonDataSerializer();

// Create multiple elections sharing the same connection pool
var elections = new List<PostgresLeaderElectionService>
{
    new(dataSource, serializer, "election-1", "participant-1"),
    new(dataSource, serializer, "election-2", "participant-2"),
    new(dataSource, serializer, "election-3", "participant-3")
};

// Start all elections concurrently
await Task.WhenAll(elections.Select(e => e.StartAsync()));
```

### Monitoring and Metrics

```csharp
public class PostgresLeaderElectionMetrics
{
    private readonly PostgresLeaderElectionService _leaderService;
    private readonly IMetrics _metrics;
    private volatile bool _isLeader = false;
    
    public PostgresLeaderElectionMetrics(
        PostgresLeaderElectionService leaderService,
        IMetrics metrics)
    {
        _leaderService = leaderService;
        _metrics = metrics;
        
        _leaderService.LeadershipChanged += OnLeadershipChanged;
    }
    
    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        _isLeader = args.IsLeader;
        
        // Record metrics
        _metrics.Gauge("leader_election.is_leader", _isLeader ? 1 : 0, new[]
        {
            new KeyValuePair<string, object?>("election_name", _leaderService.ElectionName),
            new KeyValuePair<string, object?>("participant_id", _leaderService.ParticipantId)
        });
        
        if (args.LeadershipGained)
        {
            _metrics.Counter("leader_election.leadership_gained").Increment();
        }
        else if (args.LeadershipLost)
        {
            _metrics.Counter("leader_election.leadership_lost").Increment();
        }
    }
}
```

## Debugging and Troubleshooting

### SQL Queries for Debugging

```sql
-- View all current leases
SELECT 
    election_name,
    participant_id,
    acquired_at,
    expires_at,
    expires_at > NOW() as is_valid,
    EXTRACT(EPOCH FROM (expires_at - NOW())) as seconds_to_expiry,
    metadata
FROM leader_election_leases
ORDER BY acquired_at DESC;

-- View expired leases
SELECT 
    election_name,
    participant_id,
    acquired_at,
    expires_at,
    EXTRACT(EPOCH FROM (NOW() - expires_at)) as seconds_expired,
    metadata
FROM leader_election_leases
WHERE expires_at <= NOW()
ORDER BY expires_at DESC;

-- View leases by election
SELECT 
    election_name,
    participant_id,
    acquired_at,
    expires_at,
    metadata->>'hostname' as hostname,
    metadata->>'process_id' as process_id
FROM leader_election_leases
WHERE election_name = 'your-election-name';
```

### Common Issues and Solutions

#### Connection Issues
```csharp
// Add connection retry logic
var options = new LeaderElectionOptions
{
    OperationTimeout = TimeSpan.FromSeconds(30), // Increase timeout
    LeaseDuration = TimeSpan.FromMinutes(5),     // Longer lease
    RenewalInterval = TimeSpan.FromMinutes(1)    // Less frequent renewal
};

// Enable connection logging
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableParameterLogging();
dataSourceBuilder.EnableStatisticsCollection();
```

#### Lease Conflicts
```csharp
// Debug lease conflicts
leaderService.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipLost)
    {
        var currentLeader = leaderService.CurrentLeader;
        Console.WriteLine($"Lost leadership to: {currentLeader?.ParticipantId}");
        Console.WriteLine($"Current leader acquired at: {currentLeader?.AcquiredAt}");
        Console.WriteLine($"Current leader expires at: {currentLeader?.ExpiresAt}");
    }
};
```

## Best Practices

1. **Use the Create method**: Use `LeaderElectionOptions.Create()` for optimal timing relationships instead of manually configuring intervals
2. **Use connection pooling**: Configure appropriate pool sizes for your load
3. **Monitor lease health**: Set up monitoring for lease acquisition/loss events
4. **Handle connection failures**: Implement retry logic and failover strategies
5. **Use meaningful participant IDs**: Include hostname, process ID, or other identifiers
6. **Set appropriate timeouts**: Balance between quick failover and stability using the Create method's automatic calculations
7. **Clean up expired leases**: Implement automated cleanup for better performance
8. **Use metadata effectively**: Store debugging information in lease metadata
9. **Test failover scenarios**: Ensure your application handles PostgreSQL failures gracefully
10. **Monitor database performance**: Leader election adds load to your database
11. **Use transactions carefully**: Avoid long-running transactions that might block lease operations

The PostgreSQL leader election implementation provides a robust, enterprise-grade solution for distributed coordination with the reliability and consistency guarantees of PostgreSQL.