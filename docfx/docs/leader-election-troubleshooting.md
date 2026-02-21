# Leader Election - Troubleshooting Guide

This guide covers common issues, diagnostic techniques, and solutions for leader election problems in distributed systems.

## Common Issues and Solutions

### 1. Multiple Leaders Detected

**Symptoms:**
- Multiple instances believe they are the leader
- Duplicate processing of tasks
- Data inconsistencies

**Causes:**
- Network partitions
- Clock synchronization issues
- Race conditions in lease store
- Improper lease store implementation

**Solutions:**

#### Check Clock Synchronization
```csharp
public class ClockSynchronizationChecker
{
    public async Task<bool> CheckClockSyncAsync()
    {
        try
        {
            // Compare local time with NTP server
            var ntpClient = new NtpClient();
            var networkTime = await ntpClient.GetNetworkTimeAsync();
            var localTime = DateTime.UtcNow;
            
            var timeDifference = Math.Abs((networkTime - localTime).TotalSeconds);
            
            if (timeDifference > 5) // 5 seconds tolerance
            {
                Console.WriteLine($"Clock drift detected: {timeDifference}s");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Clock sync check failed: {ex.Message}");
            return false;
        }
    }
}
```

#### Implement Lease Store Validation
```csharp
public class ValidatedLeaseStore : ILeaseStore
{
    private readonly ILeaseStore _inner;
    private readonly ILogger _logger;

    public ValidatedLeaseStore(ILeaseStore inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata, cancellationToken);
        
        if (result != null)
        {
            // Validate the lease immediately after acquisition
            var verification = await _inner.GetCurrentLeaseAsync(electionName, cancellationToken);
            
            if (verification?.ParticipantId != participantId)
            {
                _logger.LogWarning("Lease acquisition validation failed! Expected {Expected}, got {Actual}",
                    participantId, verification?.ParticipantId);
                return null;
            }
        }
        
        return result;
    }

    // Implement other methods with similar validation...
}
```

### 2. No Leader Elected

**Symptoms:**
- All instances report `IsLeader = false`
- Tasks are not being processed
- Leadership events are not fired

**Causes:**
- Lease store connectivity issues
- Overly restrictive timeouts
- Network connectivity problems
- Exception handling masking errors

**Solutions:**

#### Enhanced Logging and Diagnostics
```csharp
public class DiagnosticLeaderElectionService : ILeaderElectionService
{
    private readonly ILeaderElectionService _inner;
    private readonly ILogger _logger;
    private readonly IMetricsCollector _metrics;

    public DiagnosticLeaderElectionService(
        ILeaderElectionService inner,
        ILogger logger,
        IMetricsCollector metrics)
    {
        _inner = inner;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Attempting to acquire leadership for {ElectionName}:{ParticipantId}",
                _inner.ElectionName, _inner.ParticipantId);
            
            var result = await _inner.TryAcquireLeadershipAsync(cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Leadership acquisition {Result} for {ElectionName}:{ParticipantId} in {Duration}ms",
                result ? "SUCCESS" : "FAILED", _inner.ElectionName, _inner.ParticipantId, stopwatch.ElapsedMilliseconds);
            
            _metrics.Histogram("leader_election.acquire_duration", stopwatch.ElapsedMilliseconds);
            _metrics.Increment(result ? "leader_election.acquire_success" : "leader_election.acquire_failure");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Leadership acquisition ERROR for {ElectionName}:{ParticipantId} after {Duration}ms",
                _inner.ElectionName, _inner.ParticipantId, stopwatch.ElapsedMilliseconds);
            
            _metrics.Increment("leader_election.acquire_error");
            
            throw;
        }
    }

    // Implement other methods with similar diagnostics...
}
```

#### Connection Health Checker
```csharp
public class LeaseStoreHealthChecker
{
    private readonly ILeaseStore _leaseStore;
    private readonly ILogger _logger;

    public LeaseStoreHealthChecker(ILeaseStore leaseStore, ILogger logger)
    {
        _leaseStore = leaseStore;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthTests = new List<Task<bool>>
        {
            TestBasicConnectivity(cancellationToken),
            TestLeaseOperations(cancellationToken),
            TestPerformance(cancellationToken)
        };

        try
        {
            var results = await Task.WhenAll(healthTests);
            
            if (results.All(r => r))
            {
                return HealthCheckResult.Healthy("All lease store operations are working");
            }
            else
            {
                return HealthCheckResult.Degraded("Some lease store operations are failing");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Lease store health check failed: {ex.Message}");
        }
    }

    private async Task<bool> TestBasicConnectivity(CancellationToken cancellationToken)
    {
        try
        {
            await _leaseStore.HasValidLeaseAsync("health-check", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Basic connectivity test failed");
            return false;
        }
    }

    private async Task<bool> TestLeaseOperations(CancellationToken cancellationToken)
    {
        var testElection = $"health-check-{Guid.NewGuid()}";
        var testParticipant = "health-checker";

        try
        {
            // Test acquire
            var lease = await _leaseStore.TryAcquireLeaseAsync(
                testElection, testParticipant, TimeSpan.FromMinutes(1), null, cancellationToken);
            
            if (lease == null) return false;

            // Test renew
            var renewed = await _leaseStore.TryRenewLeaseAsync(
                testElection, testParticipant, TimeSpan.FromMinutes(1), null, cancellationToken);
            
            if (renewed == null) return false;

            // Test release
            await _leaseStore.ReleaseLeaseAsync(testElection, testParticipant, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lease operations test failed");
            return false;
        }
    }

    private async Task<bool> TestPerformance(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _leaseStore.HasValidLeaseAsync("performance-test", cancellationToken);
            stopwatch.Stop();
            
            // Should complete within 1 second
            return stopwatch.ElapsedMilliseconds < 1000;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Performance test failed");
            return false;
        }
    }
}
```

### 3. Frequent Leadership Changes

**Symptoms:**
- Leadership constantly switching between instances
- Frequent `LeadershipChanged` events
- Poor performance due to constant state changes

**Causes:**
- Lease duration too short
- Network instability
- Resource contention
- Insufficient renewal interval

**Solutions:**

#### Stability Configuration
```csharp
public static class StabilitySettings
{
    public static LeaderElectionOptions CreateStableConfiguration()
    {
        return new LeaderElectionOptions
        {
            // Longer lease duration for stability
            LeaseDuration = TimeSpan.FromMinutes(10),
            
            // Conservative renewal interval (3x safety margin)
            RenewalInterval = TimeSpan.FromMinutes(3),
            
            // Reasonable retry interval
            RetryInterval = TimeSpan.FromMinutes(1),
            
            // Generous timeout for network operations
            OperationTimeout = TimeSpan.FromMinutes(2),
            
            EnableContinuousCheck = true
        };
    }
}
```

#### Leadership Stability Monitor
```csharp
public class LeadershipStabilityMonitor
{
    private readonly List<DateTime> _leadershipChanges = new();
    private readonly object _lock = new();

    public void RecordLeadershipChange()
    {
        lock (_lock)
        {
            _leadershipChanges.Add(DateTime.UtcNow);
            
            // Keep only last hour of changes
            var cutoff = DateTime.UtcNow.AddHours(-1);
            _leadershipChanges.RemoveAll(d => d < cutoff);
        }
    }

    public StabilityMetrics GetStabilityMetrics()
    {
        lock (_lock)
        {
            var recentChanges = _leadershipChanges.Where(d => d > DateTime.UtcNow.AddMinutes(-10)).Count();
            
            return new StabilityMetrics
            {
                ChangesInLastHour = _leadershipChanges.Count,
                ChangesInLastTenMinutes = recentChanges,
                IsStable = recentChanges < 3, // Less than 3 changes in 10 minutes
                LastChangeTime = _leadershipChanges.LastOrDefault()
            };
        }
    }
}
```

### 4. Lease Store Connection Issues

**Symptoms:**
- Timeouts during lease operations
- Intermittent leadership failures
- Network-related exceptions

**Causes:**
- Database connection issues
- Redis server problems
- Network partitions
- Firewall restrictions

**Solutions:**

#### Redis-Specific Connection Troubleshooting

```csharp
public class RedisConnectionDiagnostics
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public RedisConnectionDiagnostics(IConnectionMultiplexer redis, ILogger logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RedisDiagnosticResult> DiagnoseAsync()
    {
        var result = new RedisDiagnosticResult();
        
        try
        {
            // Test basic connectivity
            var database = _redis.GetDatabase();
            var pingResult = await database.PingAsync();
            result.PingLatency = pingResult;
            result.IsConnected = true;

            // Test Redis server info
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var info = await server.InfoAsync();
            result.ServerInfo = info.ToDictionary(x => x.Key, x => x.Value);

            // Test key operations
            var testKey = $"health-check:{Guid.NewGuid()}";
            await database.StringSetAsync(testKey, "test-value", TimeSpan.FromSeconds(30));
            var getValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);
            
            result.KeyOperationsWorking = getValue == "test-value";

            // Check Redis configuration
            var config = await server.ConfigGetAsync();
            result.RedisConfig = config.ToDictionary(x => x.Key, x => x.Value);

            // Memory usage check
            var memoryInfo = info.FirstOrDefault(x => x.Key.StartsWith("used_memory"));
            if (memoryInfo != null)
            {
                result.MemoryUsage = memoryInfo.Value;
            }

            result.IsHealthy = result.IsConnected && result.KeyOperationsWorking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis diagnostics failed");
            result.IsHealthy = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class RedisDiagnosticResult
{
    public bool IsHealthy { get; set; }
    public bool IsConnected { get; set; }
    public TimeSpan PingLatency { get; set; }
    public bool KeyOperationsWorking { get; set; }
    public Dictionary<string, string> ServerInfo { get; set; } = new();
    public Dictionary<string, string> RedisConfig { get; set; } = new();
    public string? MemoryUsage { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### Redis Connection Resilience

```csharp
public class ResilientRedisLeaderElectionService : ILeaderElectionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDataSerializer _serializer;
    private readonly RedisLeaderElectionService _innerService;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _reconnectSemaphore;
    private volatile bool _isConnected = true;

    public ResilientRedisLeaderElectionService(
        IConnectionMultiplexer redis,
        IDataSerializer serializer,
        string electionName,
        string participantId,
        RedisLeaderElectionOptions? options = null,
        ILogger? logger = null)
    {
        _redis = redis;
        _serializer = serializer;
        _logger = logger ?? NullLogger.Instance;
        _reconnectSemaphore = new SemaphoreSlim(1, 1);

        _innerService = new RedisLeaderElectionService(
            redis, serializer, electionName, participantId, options, logger);

        // Monitor connection events
        _redis.ConnectionFailed += OnConnectionFailed;
        _redis.ConnectionRestored += OnConnectionRestored;
    }

    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        _isConnected = false;
        _logger.LogError(e.Exception, "Redis connection failed: {FailureType}", e.FailureType);
        
        // Trigger reconnection attempt
        _ = Task.Run(AttemptReconnection);
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        _isConnected = true;
        _logger.LogInformation("Redis connection restored");
    }

    private async Task AttemptReconnection()
    {
        await _reconnectSemaphore.WaitAsync();
        try
        {
            if (_isConnected) return;

            var retryCount = 0;
            var maxRetries = 5;
            var delay = TimeSpan.FromSeconds(1);

            while (retryCount < maxRetries && !_isConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    await db.PingAsync();
                    _isConnected = true;
                    _logger.LogInformation("Redis reconnection successful after {RetryCount} attempts", retryCount + 1);
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "Redis reconnection attempt {RetryCount} failed", retryCount);
                    
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 30000));
                    }
                }
            }
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public async Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("Attempting leadership acquisition while Redis is disconnected");
            return false;
        }

        try
        {
            return await _innerService.TryAcquireLeadershipAsync(cancellationToken);
        }
        catch (Exception ex) when (IsRedisConnectionException(ex))
        {
            _logger.LogWarning(ex, "Redis connection issue during leadership acquisition");
            _isConnected = false;
            _ = Task.Run(AttemptReconnection);
            return false;
        }
    }

    private static bool IsRedisConnectionException(Exception ex)
    {
        return ex is RedisConnectionException ||
               ex is RedisTimeoutException ||
               ex is SocketException ||
               ex.Message.Contains("connection");
    }

    // Forward other methods with similar resilience patterns...
    public string ParticipantId => _innerService.ParticipantId;
    public string ElectionName => _innerService.ElectionName;
    public bool IsLeader => _innerService.IsLeader;
    public LeaderInfo? CurrentLeader => _innerService.CurrentLeader;
    public event EventHandler<LeadershipChangedEventArgs>? LeadershipChanged
    {
        add => _innerService.LeadershipChanged += value;
        remove => _innerService.LeadershipChanged -= value;
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => _innerService.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => _innerService.StopAsync(cancellationToken);
    public Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default) => _innerService.ReleaseLeadershipAsync(cancellationToken);
    public ValueTask DisposeAsync() => _innerService.DisposeAsync();
}
```

#### Redis Cluster Troubleshooting

```csharp
public class RedisClusterDiagnostics
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public RedisClusterDiagnostics(IConnectionMultiplexer redis, ILogger logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<ClusterDiagnosticResult> DiagnoseClusterAsync()
    {
        var result = new ClusterDiagnosticResult();
        
        try
        {
            var endpoints = _redis.GetEndPoints();
            result.TotalNodes = endpoints.Length;

            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var nodeInfo = new RedisNodeInfo
                {
                    Endpoint = endpoint.ToString(),
                    IsConnected = server.IsConnected,
                    IsSlave = server.IsSlave,
                    ServerType = server.ServerType.ToString()
                };

                try
                {
                    var info = await server.InfoAsync();
                    nodeInfo.Role = info.FirstOrDefault(x => x.Key == "role")?.Value;
                    nodeInfo.ConnectedClients = info.FirstOrDefault(x => x.Key == "connected_clients")?.Value;
                    
                    if (server.IsConnected)
                    {
                        var ping = await server.PingAsync();
                        nodeInfo.PingLatency = ping;
                        result.HealthyNodes++;
                    }
                }
                catch (Exception ex)
                {
                    nodeInfo.ErrorMessage = ex.Message;
                    _logger.LogWarning(ex, "Failed to get info from Redis node {Endpoint}", endpoint);
                }

                result.Nodes.Add(nodeInfo);
            }

            result.IsHealthy = result.HealthyNodes >= (result.TotalNodes / 2) + 1; // Majority healthy
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis cluster diagnostics failed");
            result.IsHealthy = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class ClusterDiagnosticResult
{
    public bool IsHealthy { get; set; }
    public int TotalNodes { get; set; }
    public int HealthyNodes { get; set; }
    public List<RedisNodeInfo> Nodes { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class RedisNodeInfo
{
    public string Endpoint { get; set; } = "";
    public bool IsConnected { get; set; }
    public bool IsSlave { get; set; }
    public string ServerType { get; set; } = "";
    public string? Role { get; set; }
    public string? ConnectedClients { get; set; }
    public TimeSpan PingLatency { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### Retry and Circuit Breaker Pattern
```csharp
public class ResilientLeaseStore : ILeaseStore
{
    private readonly ILeaseStore _inner;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly ILogger _logger;

    public ResilientLeaseStore(
        ILeaseStore inner,
        IRetryPolicy retryPolicy,
        ICircuitBreaker circuitBreaker,
        ILogger logger)
    {
        _inner = inner;
        _retryPolicy = retryPolicy;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public async Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    return await _inner.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata, cancellationToken);
                }
                catch (Exception ex) when (IsTransientException(ex))
                {
                    _logger.LogWarning(ex, "Transient error during lease acquisition, will retry");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Non-transient error during lease acquisition");
                    return null; // Don't retry non-transient errors
                }
            });
        });
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is TimeoutException ||
               ex is OperationCanceledException ||
               ex is SocketException ||
               ex is HttpRequestException ||
               (ex is NpgsqlException sqlEx && IsTransientNpgsqlException(sqlEx));
    }

    private static bool IsTransientNpgsqlException(NpgsqlException sqlEx)
    {
        var transientErrorNumbers = new[] { 2, 20, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613 };
        return transientErrorNumbers.Contains(sqlEx.Number);
    }
}
```

### 5. Incomplete Event Information

**Symptoms:**
- `NullReferenceException` when accessing `args.CurrentLeader` or `args.PreviousLeader`
- Missing leader information in event handlers
- Inconsistent leadership data in event arguments

**Causes:**
- Lease store operation failures
- Network connectivity issues during leader determination
- Error conditions that prevent accurate leader information retrieval

**Solutions:**

#### Always Use Service Properties for Reliable Information

```csharp
// ❌ Wrong - unreliable event data access
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        // This can throw NullReferenceException
        Console.WriteLine($"New leader: {args.CurrentLeader.ParticipantId}");
    }
};

// ✅ Correct - safe access with service properties
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        var service = (ILeaderElectionService)sender!;
        var currentLeader = service.CurrentLeader;
        
        if (currentLeader != null)
        {
            Console.WriteLine($"New leader: {currentLeader.ParticipantId}");
        }
        else
        {
            Console.WriteLine($"Leadership gained by {service.ParticipantId} but leader info unavailable");
        }
    }
};
```

#### Defensive Event Handling Pattern

```csharp
public class SafeLeadershipEventHandler
{
    private readonly ILeaderElectionService _service;
    private readonly ILogger _logger;

    public SafeLeadershipEventHandler(ILeaderElectionService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
        
        service.LeadershipChanged += OnLeadershipChanged;
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        try
        {
            // Use computed properties which are always reliable
            var isLeader = args.IsLeader;
            var leadershipGained = args.LeadershipGained;
            var leadershipLost = args.LeadershipLost;

            if (leadershipGained)
            {
                _logger.LogInformation("Leadership gained");
                
                // Always get current state from service, not from event args
                var actualCurrentLeader = _service.CurrentLeader;
                var isActuallyLeader = _service.IsLeader;
                
                if (actualCurrentLeader != null && isActuallyLeader)
                {
                    HandleLeadershipGained(actualCurrentLeader);
                }
                else
                {
                    _logger.LogWarning("Leadership gained event fired but service state inconsistent");
                }
            }
            else if (leadershipLost)
            {
                _logger.LogInformation("Leadership lost");
                HandleLeadershipLost();
                
                // Check who the new leader is (if any)
                var newLeader = _service.CurrentLeader;
                if (newLeader != null)
                {
                    _logger.LogInformation("New leader: {LeaderId}", newLeader.ParticipantId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling leadership change event");
        }
    }

    private void HandleLeadershipGained(LeaderInfo leaderInfo)
    {
        // Safe to use leaderInfo here as we've verified it's not null
        _logger.LogInformation("Starting leader operations for {ParticipantId}", leaderInfo.ParticipantId);
    }

    private void HandleLeadershipLost()
    {
        _logger.LogInformation("Stopping leader operations");
    }
}
```

### 6. Memory Leaks and Resource Issues

**Symptoms:**
- Increasing memory usage over time
- Handle/connection leaks
- Poor garbage collection performance

**Causes:**
- Not disposing services properly
- Event handler subscriptions not cleaned up
- Long-running timers not disposed

**Solutions:**

#### Proper Resource Management
```csharp
public class ResourceManagedLeaderElectionService : ILeaderElectionService, IDisposable
{
    private readonly ILeaderElectionService _inner;
    private readonly Timer _healthCheckTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public ResourceManagedLeaderElectionService(ILeaderElectionService inner)
    {
        _inner = inner;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Set up health check timer
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        // Subscribe to events
        _inner.LeadershipChanged += OnLeadershipChanged;
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs e)
    {
        LeadershipChanged?.Invoke(sender, e);
    }

    private async void PerformHealthCheck(object? state)
    {
        if (_disposed) return;
        
        try
        {
            // Check if service is still responsive
            var isHealthy = await CheckServiceHealthAsync();
            if (!isHealthy)
            {
                // Log warning or trigger alerts
                Console.WriteLine("Leader election service health check failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check error: {ex.Message}");
        }
    }

    private async Task<bool> CheckServiceHealthAsync()
    {
        try
        {
            // Simple health check - try to get current leader info
            var _ = _inner.CurrentLeader;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Unsubscribe from events
            _inner.LeadershipChanged -= OnLeadershipChanged;
            
            // Dispose timers
            _healthCheckTimer?.Dispose();
            
            // Cancel any ongoing operations
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            // Dispose inner service
            _inner?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
            
            _disposed = true;
        }
    }

    // Forward all other members to inner service
    public string ParticipantId => _inner.ParticipantId;
    public string ElectionName => _inner.ElectionName;
    public bool IsLeader => _inner.IsLeader;
    public LeaderInfo? CurrentLeader => _inner.CurrentLeader;
    public event EventHandler<LeadershipChangedEventArgs>? LeadershipChanged;

    public Task StartAsync(CancellationToken cancellationToken = default) => _inner.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => _inner.StopAsync(cancellationToken);
    public Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default) => _inner.TryAcquireLeadershipAsync(cancellationToken);
    public Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default) => _inner.ReleaseLeadershipAsync(cancellationToken);
    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
```

## Diagnostic Tools

### 1. Leadership Status Dashboard

```csharp
public class LeadershipStatusDashboard
{
    private readonly ILeaderElectionService _service;
    private readonly ILogger _logger;

    public LeadershipStatusDashboard(ILeaderElectionService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<LeadershipStatus> GetStatusAsync()
    {
        try
        {
            var status = new LeadershipStatus
            {
                ElectionName = _service.ElectionName,
                ParticipantId = _service.ParticipantId,
                IsLeader = _service.IsLeader,
                CurrentLeader = _service.CurrentLeader,
                Timestamp = DateTime.UtcNow
            };

            if (status.CurrentLeader != null)
            {
                status.LeadershipDuration = DateTime.UtcNow - status.CurrentLeader.AcquiredAt;
                status.TimeToExpiry = status.CurrentLeader.TimeToExpiry;
                status.IsLeaseValid = status.CurrentLeader.IsValid;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leadership status");
            throw;
        }
    }

    public async Task PrintStatusAsync()
    {
        var status = await GetStatusAsync();
        
        Console.WriteLine($"=== Leadership Status ===");
        Console.WriteLine($"Election: {status.ElectionName}");
        Console.WriteLine($"Participant: {status.ParticipantId}");
        Console.WriteLine($"Is Leader: {status.IsLeader}");
        Console.WriteLine($"Current Leader: {status.CurrentLeader?.ParticipantId ?? "None"}");
        
        if (status.CurrentLeader != null)
        {
            Console.WriteLine($"Leader Since: {status.CurrentLeader.AcquiredAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Leadership Duration: {status.LeadershipDuration}");
            Console.WriteLine($"Lease Expires: {status.CurrentLeader.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Time to Expiry: {status.TimeToExpiry}");
            Console.WriteLine($"Lease Valid: {status.IsLeaseValid}");
            
            if (status.CurrentLeader.Metadata != null && status.CurrentLeader.Metadata.Any())
            {
                Console.WriteLine($"Metadata:");
                foreach (var (key, value) in status.CurrentLeader.Metadata)
                {
                    Console.WriteLine($"  {key}: {value}");
                }
            }
        }
        
        Console.WriteLine($"Status Retrieved: {status.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
    }
}

public class LeadershipStatus
{
    public string ElectionName { get; set; } = "";
    public string ParticipantId { get; set; } = "";
    public bool IsLeader { get; set; }
    public LeaderInfo? CurrentLeader { get; set; }
    public TimeSpan? LeadershipDuration { get; set; }
    public TimeSpan? TimeToExpiry { get; set; }
    public bool IsLeaseValid { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 2. Network Connectivity Tester

```csharp
public class NetworkConnectivityTester
{
    private readonly ILogger _logger;

    public NetworkConnectivityTester(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<NetworkTestResult> TestConnectivityAsync(string[] testHosts)
    {
        var results = new List<HostTestResult>();
        
        foreach (var host in testHosts)
        {
            results.Add(await TestHostAsync(host));
        }

        return new NetworkTestResult
        {
            Results = results,
            OverallHealth = results.All(r => r.IsHealthy) ? "Healthy" : "Degraded",
            TestTime = DateTime.UtcNow
        };
    }

    private async Task<HostTestResult> TestHostAsync(string host)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 5000);
            
            stopwatch.Stop();
            
            return new HostTestResult
            {
                Host = host,
                IsHealthy = reply.Status == IPStatus.Success,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Status = reply.Status.ToString(),
                Error = reply.Status != IPStatus.Success ? reply.Status.ToString() : null
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return new HostTestResult
            {
                Host = host,
                IsHealthy = false,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Status = "Exception",
                Error = ex.Message
            };
        }
    }
}

public class NetworkTestResult
{
    public List<HostTestResult> Results { get; set; } = new();
    public string OverallHealth { get; set; } = "";
    public DateTime TestTime { get; set; }
}

public class HostTestResult
{
    public string Host { get; set; } = "";
    public bool IsHealthy { get; set; }
    public long ResponseTime { get; set; }
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}
```

### 3. Performance Profiler

```csharp
public class LeaderElectionPerformanceProfiler
{
    private readonly Dictionary<string, List<long>> _operationTimes = new();
    private readonly object _lock = new();

    public void RecordOperation(string operationName, long milliseconds)
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(operationName))
            {
                _operationTimes[operationName] = new List<long>();
            }
            
            _operationTimes[operationName].Add(milliseconds);
            
            // Keep only last 100 measurements
            if (_operationTimes[operationName].Count > 100)
            {
                _operationTimes[operationName].RemoveAt(0);
            }
        }
    }

    public PerformanceReport GetPerformanceReport()
    {
        lock (_lock)
        {
            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                Operations = new Dictionary<string, OperationStats>()
            };

            foreach (var (operationName, times) in _operationTimes)
            {
                if (times.Count > 0)
                {
                    report.Operations[operationName] = new OperationStats
                    {
                        Count = times.Count,
                        AverageMs = times.Average(),
                        MinMs = times.Min(),
                        MaxMs = times.Max(),
                        P95Ms = CalculatePercentile(times, 95),
                        P99Ms = CalculatePercentile(times, 99)
                    };
                }
            }

            return report;
        }
    }

    private static double CalculatePercentile(List<long> values, double percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}

public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, OperationStats> Operations { get; set; } = new();
}

public class OperationStats
{
    public int Count { get; set; }
    public double AverageMs { get; set; }
    public long MinMs { get; set; }
    public long MaxMs { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
}
```

## Prevention Strategies

### 1. Configuration Validation

```csharp
public static class LeaderElectionOptionsValidator
{
    public static ValidationResult ValidateOptions(LeaderElectionOptions options)
    {
        var issues = new List<string>();

        // Validate timing relationships
        if (options.RenewalInterval >= options.LeaseDuration)
        {
            issues.Add("RenewalInterval must be less than LeaseDuration");
        }

        if (options.RenewalInterval < TimeSpan.FromSeconds(5))
        {
            issues.Add("RenewalInterval should be at least 5 seconds to avoid excessive network traffic");
        }

        if (options.LeaseDuration > TimeSpan.FromHours(1))
        {
            issues.Add("LeaseDuration should not exceed 1 hour to ensure reasonable failover times");
        }

        if (options.OperationTimeout >= options.RenewalInterval)
        {
            issues.Add("OperationTimeout should be less than RenewalInterval");
        }

        // Validate metadata
        if (options.Metadata != null)
        {
            foreach (var (key, value) in options.Metadata)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    issues.Add("Metadata keys cannot be null or whitespace");
                }
                
                if (value?.Length > 1000)
                {
                    issues.Add($"Metadata value for key '{key}' is too long (max 1000 characters)");
                }
            }
        }

        return new ValidationResult
        {
            IsValid = issues.Count == 0,
            Issues = issues
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
}
```

### 2. Monitoring and Alerting Setup

```csharp
public class LeaderElectionMonitoring
{
    private readonly IMetricsCollector _metrics;
    private readonly IAlertManager _alerts;

    public LeaderElectionMonitoring(IMetricsCollector metrics, IAlertManager alerts)
    {
        _metrics = metrics;
        _alerts = alerts;
    }

    public void SetupMonitoring(ILeaderElectionService service)
    {
        service.LeadershipChanged += OnLeadershipChanged;
        
        // Set up periodic health checks
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await CheckHealthAsync(service);
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    _alerts.SendAlert($"Monitoring error: {ex.Message}");
                }
            }
        });
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            _metrics.Increment("leader_election.leadership_gained");
            
            // Note: args.CurrentLeader might be null in error scenarios
            // Use service properties for reliable information
            var service = (ILeaderElectionService)sender!;
            var currentLeader = service.CurrentLeader;
            _alerts.SendInfo($"Leadership gained by {currentLeader?.ParticipantId ?? service.ParticipantId}");
        }
        else if (args.LeadershipLost)
        {
            _metrics.Increment("leader_election.leadership_lost");
            _alerts.SendWarning($"Leadership lost by participant");
        }
    }

    private async Task CheckHealthAsync(ILeaderElectionService service)
    {
        var currentLeader = service.CurrentLeader;
        
        if (currentLeader != null)
        {
            var timeToExpiry = currentLeader.TimeToExpiry;
            
            if (timeToExpiry < TimeSpan.FromMinutes(1))
            {
                _alerts.SendWarning($"Leader lease expiring soon: {timeToExpiry.TotalSeconds}s remaining");
            }
        }
        
        // Check for no leader situation
        if (currentLeader == null)
        {
            _alerts.SendWarning("No leader detected in election");
        }
    }
}
```

## Redis-Specific Troubleshooting

### Redis Common Issues and Solutions

#### 1. Redis Key Expiration Issues

**Problem**: Leaders lose leadership unexpectedly due to key expiration.

**Solutions**:
```csharp
public class RedisKeyExpirationMonitor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public async Task<TimeSpan?> GetKeyTTLAsync(string key)
    {
        var database = _redis.GetDatabase();
        var ttl = await database.KeyTimeToLiveAsync(key);
        return ttl;
    }

    public async Task<List<string>> GetExpiringKeysAsync(string pattern = "leader-election:*", TimeSpan threshold = default)
    {
        if (threshold == default) threshold = TimeSpan.FromMinutes(1);
        
        var expiringKeys = new List<string>();
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        var keys = server.Keys(pattern: pattern);
        
        foreach (var key in keys)
        {
            var ttl = await GetKeyTTLAsync(key);
            if (ttl.HasValue && ttl.Value < threshold)
            {
                expiringKeys.Add(key);
            }
        }
        
        return expiringKeys;
    }
}
```

#### 2. Redis Memory Pressure

**Problem**: Redis running out of memory affects leader election.

**Solutions**:
```csharp
public class RedisMemoryMonitor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public async Task<RedisMemoryInfo> GetMemoryInfoAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        var info = await server.InfoAsync("memory");
        
        return new RedisMemoryInfo
        {
            UsedMemory = GetInfoValue(info, "used_memory"),
            MaxMemory = GetInfoValue(info, "maxmemory"),
            MemoryUsagePercentage = CalculateMemoryUsagePercentage(info),
            EvictedKeys = GetInfoValue(info, "evicted_keys"),
            ExpiredKeys = GetInfoValue(info, "expired_keys")
        };
    }

    private string GetInfoValue(IGrouping<string, KeyValuePair<string, string>>[] info, string key)
    {
        return info.SelectMany(g => g).FirstOrDefault(kvp => kvp.Key == key).Value ?? "0";
    }

    private double CalculateMemoryUsagePercentage(IGrouping<string, KeyValuePair<string, string>>[] info)
    {
        var used = long.Parse(GetInfoValue(info, "used_memory"));
        var max = long.Parse(GetInfoValue(info, "maxmemory"));
        
        return max > 0 ? (double)used / max * 100 : 0;
    }
}

public class RedisMemoryInfo
{
    public string UsedMemory { get; set; } = "";
    public string MaxMemory { get; set; } = "";
    public double MemoryUsagePercentage { get; set; }
    public string EvictedKeys { get; set; } = "";
    public string ExpiredKeys { get; set; } = "";
}
```

#### 3. Redis Lua Script Errors

**Problem**: Lua scripts in Redis lease operations fail.

**Solutions**:
```csharp
public class RedisLuaScriptValidator
{
    private readonly IConnectionMultiplexer _redis;

    public async Task<bool> ValidateLeaseRenewalScriptAsync()
    {
        var database = _redis.GetDatabase();
        
        const string testScript = @"
            local key = KEYS[1]
            local participant = ARGV[1]
            local duration = ARGV[2]
            local newData = ARGV[3]
            
            local current = redis.call('GET', key)
            if current then
                local ok, data = pcall(cjson.decode, current)
                if ok and data.ParticipantId == participant then
                    redis.call('SETEX', key, duration, newData)
                    return newData
                end
            end
            return nil";

        try
        {
            var testKey = $"script-test:{Guid.NewGuid()}";
            var testData = @"{""ParticipantId"":""test"",""AcquiredAt"":""2023-01-01T00:00:00Z""}";
            
            // Set up test data
            await database.StringSetAsync(testKey, testData, TimeSpan.FromSeconds(30));
            
            // Execute script
            var result = await database.ScriptEvaluateAsync(
                testScript,
                new RedisKey[] { testKey },
                new RedisValue[] { "test", "30", testData });
            
            // Clean up
            await database.KeyDeleteAsync(testKey);
            
            return result.HasValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lua script validation failed: {ex.Message}", ex);
        }
    }
}
```

#### 4. Redis Sentinel Failover Issues

**Problem**: Sentinel failover disrupts leader election.

**Solutions**:
```csharp
public class RedisSentinelMonitor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;

    public async Task<SentinelStatus> GetSentinelStatusAsync()
    {
        var status = new SentinelStatus();
        
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (server.ServerType == ServerType.Sentinel)
                {
                    try
                    {
                        var sentinelInfo = await server.SentinelGetMasterAddressByNameAsync("mymaster");
                        status.MasterEndpoint = sentinelInfo?.ToString();
                        status.IsHealthy = true;
                    }
                    catch (Exception ex)
                    {
                        status.ErrorMessage = ex.Message;
                        _logger.LogWarning(ex, "Failed to get sentinel master info from {Endpoint}", endpoint);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
        }
        
        return status;
    }
}

public class SentinelStatus
{
    public bool IsHealthy { get; set; }
    public string? MasterEndpoint { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Redis Best Practices for Leader Election

1. **Use appropriate key prefixes** to avoid conflicts
2. **Monitor Redis memory usage** regularly
3. **Set up proper Redis clustering** for high availability
4. **Use Redis Sentinel** for automatic failover
5. **Implement proper error handling** for Redis connection issues
6. **Monitor lease expiration times** to prevent unexpected leadership loss
7. **Use Redis AUTH** and SSL/TLS for security
8. **Configure Redis persistence** appropriately for your use case

### Redis Performance Tuning

```csharp
public class RedisPerformanceTuner
{
    public static ConfigurationOptions CreateOptimizedConfiguration(string connectionString)
    {
        return new ConfigurationOptions
        {
            EndPoints = { connectionString },
            
            // Connection settings
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            AsyncTimeout = 5000,
            
            // Retry and resilience
            ConnectRetry = 3,
            ReconnectRetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1)),
            
            // Performance optimization
            AbortOnConnectFail = false,
            KeepAlive = 60,
            
            // Leader election specific
            CommandMap = CommandMap.Default,
            DefaultDatabase = 0,
            
            // Security
            Ssl = connectionString.Contains("ssl=true"),
            SslProtocols = SslProtocols.Tls12,
            
            // Monitoring
            IncludeDetailInExceptions = true,
            IncludePerformanceCountersInExceptions = true
        };
    }
}
```

This troubleshooting guide provides comprehensive solutions for common leader election issues and tools for diagnosing problems in production environments. The Redis-specific sections help address distributed coordination challenges unique to Redis-based implementations. Use these techniques to ensure robust and reliable leader election in your distributed systems.