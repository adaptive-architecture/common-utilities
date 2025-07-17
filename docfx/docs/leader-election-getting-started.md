# Leader Election - Getting Started

This guide will walk you through the basics of implementing leader election in your distributed applications using the Common.Utilities leader election utilities.

## Installation

### For Single-Application Scenarios

The leader election utilities are part of the `AdaptArch.Common.Utilities` NuGet package:

```xml
<PackageReference Include="AdaptArch.Common.Utilities" Version="1.0.0" />
```

### For Distributed Scenarios (Multiple Applications/Machines)

For distributed leader election across multiple applications or machines, you'll also need the Redis package:

```xml
<PackageReference Include="AdaptArch.Common.Utilities" Version="1.0.0" />
<PackageReference Include="AdaptArch.Common.Utilities.Redis" Version="1.0.0" />
```

## Quick Start

Choose your implementation based on your needs:
- **In-Process**: Multiple services within the same application
- **Redis-Based**: Multiple applications across different machines

### Step 1: Basic Setup (In-Process)

```csharp
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

// Create a leader election service
await using var leaderService = new InProcessLeaderElectionService(
    electionName: "my-first-election",
    participantId: "instance-1");

// Check if we can become leader
bool isLeader = await leaderService.TryAcquireLeadershipAsync();

if (isLeader)
{
    Console.WriteLine("I am the leader!");
    // Perform leader-only operations
}
else
{
    Console.WriteLine("Someone else is the leader");
}
```

### Step 1b: Basic Setup (Redis-Based Distributed)

```csharp
using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using StackExchange.Redis;

// Connect to Redis
var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
var serializer = new ReflectionJsonDataSerializer();

// Create distributed leader election service
await using var leaderService = new RedisLeaderElectionService(
    connectionMultiplexer: connectionMultiplexer,
    serializer: serializer,
    electionName: "my-distributed-election",
    participantId: $"{Environment.MachineName}-{Environment.ProcessId}");

// Check if we can become leader across the distributed system
bool isLeader = await leaderService.TryAcquireLeadershipAsync();

if (isLeader)
{
    Console.WriteLine("I am the distributed leader!");
    // Perform leader-only operations across the entire system
}
else
{
    Console.WriteLine("Someone else is the distributed leader");
}
```

### Step 2: Handling Leadership Changes

```csharp
await using var leaderService = new InProcessLeaderElectionService(
    electionName: "my-election",
    participantId: "instance-1");

// Subscribe to leadership changes
leaderService.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        Console.WriteLine($"🎉 I became the leader!");
        
        // Use service.CurrentLeader for reliable leader information
        var currentLeader = leaderService.CurrentLeader;
        if (currentLeader != null)
        {
            Console.WriteLine($"Leader: {currentLeader.ParticipantId}, acquired at: {currentLeader.AcquiredAt}");
        }
        
        StartLeaderWork();
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine($"😔 I lost leadership");
        
        // Note: args.CurrentLeader might be null in error scenarios
        // Always use the service properties for reliable information
        var actualCurrentLeader = leaderService.CurrentLeader;
        if (actualCurrentLeader != null)
        {
            Console.WriteLine($"New leader: {actualCurrentLeader.ParticipantId}");
        }
        
        StopLeaderWork();
    }
};

// Try to acquire leadership
await leaderService.TryAcquireLeadershipAsync();
```

> **⚠️ Important**: The `LeadershipChangedEventArgs` may not always contain complete information about the current and previous leader, especially during error conditions or lease failures. Always use the service's `IsLeader` and `CurrentLeader` properties for the most reliable information.

### Step 3: Automatic Leadership Management

```csharp
var options = new LeaderElectionOptions
{
    EnableContinuousCheck = true,                           // Enable automatic management
    LeaseDuration = TimeSpan.FromMinutes(2),    // Leadership lasts 2 minutes
    RenewalInterval = TimeSpan.FromSeconds(30), // Renew every 30 seconds
    RetryInterval = TimeSpan.FromSeconds(10)    // Retry every 10 seconds if not leader
};

await using var leaderService = new InProcessLeaderElectionService(
    electionName: "auto-managed-election",
    participantId: Environment.MachineName,
    options: options);

// Set up event handlers
leaderService.LeadershipChanged += OnLeadershipChanged;

// Start the automatic election process
await leaderService.StartAsync();

// Your application continues running...
Console.WriteLine("Press any key to stop the election...");
Console.ReadKey();

// Stop the election
await leaderService.StopAsync();

static void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
{
    if (args.LeadershipGained)
    {
        Console.WriteLine("Starting background work as leader...");
        // Start your leader-only operations
    }
    else if (args.LeadershipLost)
    {
        Console.WriteLine("Stopping background work - no longer leader...");
        // Stop your leader-only operations
    }
}
```

## Common Patterns

### Pattern 1: Background Service with Leader Election

```csharp
public class LeaderElectedBackgroundService : BackgroundService
{
    private readonly ILeaderElectionService _leaderService;
    private readonly IJobProcessor _jobProcessor;
    private volatile bool _isLeader = false;

    public LeaderElectedBackgroundService(IJobProcessor jobProcessor)
    {
        _jobProcessor = jobProcessor;
        _leaderService = new InProcessLeaderElectionService(
            "background-service",
            Environment.MachineName,
            new LeaderElectionOptions
            {
                EnableContinuousCheck = true,
                LeaseDuration = TimeSpan.FromMinutes(3),
                RenewalInterval = TimeSpan.FromMinutes(1)
            });

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
                // Process jobs only when we're the leader
                await _jobProcessor.ProcessPendingJobsAsync(stoppingToken);
            }
            
            // Wait before next iteration
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        _isLeader = args.IsLeader;
        
        if (args.LeadershipGained)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Became leader - starting job processing");
        }
        else if (args.LeadershipLost)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Lost leadership - stopping job processing");
        }
    }
}
```

### Pattern 2: Manual Leadership for Specific Operations

```csharp
public class DataMigrationService
{
    private readonly ILeaderElectionService _leaderService;
    private readonly IDataRepository _repository;

    public DataMigrationService(IDataRepository repository)
    {
        _repository = repository;
        _leaderService = new InProcessLeaderElectionService(
            "data-migration",
            $"{Environment.MachineName}-{Environment.ProcessId}",
            new LeaderElectionOptions
            {
                EnableContinuousCheck = false,  // Manual control
                LeaseDuration = TimeSpan.FromMinutes(10),
                OperationTimeout = TimeSpan.FromMinutes(2)
            });
    }

    public async Task<bool> TryRunMigrationAsync(CancellationToken cancellationToken = default)
    {
        // Try to acquire leadership for this specific operation
        bool becameLeader = await _leaderService.TryAcquireLeadershipAsync(cancellationToken);
        
        if (!becameLeader)
        {
            Console.WriteLine("Another instance is already running the migration");
            return false;
        }

        try
        {
            Console.WriteLine("Starting data migration as leader...");
            
            // Perform the migration
            await _repository.MigrateDataAsync(cancellationToken);
            
            Console.WriteLine("Data migration completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");
            throw;
        }
        finally
        {
            // Always release leadership when done
            await _leaderService.ReleaseLeadershipAsync();
        }
    }
}
```

### Pattern 3: Heartbeat and Health Monitoring

```csharp
public class HealthMonitoringService
{
    private readonly ILeaderElectionService _leaderService;
    private readonly IHealthChecker _healthChecker;
    private readonly INotificationService _notifications;
    private Timer? _monitoringTimer;

    public HealthMonitoringService(
        IHealthChecker healthChecker,
        INotificationService notifications)
    {
        _healthChecker = healthChecker;
        _notifications = notifications;
        
        _leaderService = new InProcessLeaderElectionService(
            "health-monitor",
            Environment.MachineName,
            new LeaderElectionOptions
            {
                EnableContinuousCheck = true,
                LeaseDuration = TimeSpan.FromMinutes(2),
                RenewalInterval = TimeSpan.FromSeconds(30),
                // Add metadata to identify this instance
                Metadata = new Dictionary<string, string>
                {
                    ["Version"] = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
                    ["StartTime"] = DateTime.UtcNow.ToString("O")
                }
            });

        _leaderService.LeadershipChanged += OnLeadershipChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _leaderService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _monitoringTimer?.Dispose();
        await _leaderService.StopAsync(cancellationToken);
        await _leaderService.DisposeAsync();
    }

    private void OnLeadershipChanged(object? sender, LeadershipChangedEventArgs args)
    {
        if (args.LeadershipGained)
        {
            Console.WriteLine("Starting health monitoring as leader...");
            // Start monitoring every 30 seconds
            _monitoringTimer = new Timer(CheckHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        else if (args.LeadershipLost)
        {
            Console.WriteLine("Stopping health monitoring - no longer leader");
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }
    }

    private async void CheckHealth(object? state)
    {
        // Double-check we're still the leader
        if (!_leaderService.IsLeader) return;

        try
        {
            var healthStatus = await _healthChecker.CheckSystemHealthAsync();
            
            if (!healthStatus.IsHealthy)
            {
                await _notifications.SendAlertAsync(
                    "System Health Alert",
                    $"Health check failed: {healthStatus.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check failed: {ex.Message}");
        }
    }
}
```

## Working with Multiple Participants

### Testing Leader Election Locally

```csharp
public static async Task TestMultipleParticipantsAsync()
{
    var participants = new List<ILeaderElectionService>();
    
    try
    {
        // Create multiple participants
        for (int i = 1; i <= 3; i++)
        {
            var participant = new InProcessLeaderElectionService(
                "test-election",
                $"participant-{i}",
                new LeaderElectionOptions
                {
                    EnableContinuousCheck = true,
                    LeaseDuration = TimeSpan.FromSeconds(30),
                    RenewalInterval = TimeSpan.FromSeconds(10),
                    RetryInterval = TimeSpan.FromSeconds(5)
                });

            participant.LeadershipChanged += (sender, args) =>
            {
                var service = (ILeaderElectionService)sender!;
                if (args.LeadershipGained)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {service.ParticipantId} became leader");
                }
                else if (args.LeadershipLost)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {service.ParticipantId} lost leadership");
                }
            };

            participants.Add(participant);
        }

        // Start all participants
        await Task.WhenAll(participants.Select(p => p.StartAsync()));

        Console.WriteLine("All participants started. Press any key to stop the first leader...");
        Console.ReadKey();

        // Stop the current leader to trigger failover
        var currentLeader = participants.FirstOrDefault(p => p.IsLeader);
        if (currentLeader != null)
        {
            Console.WriteLine($"Stopping current leader: {currentLeader.ParticipantId}");
            await currentLeader.StopAsync();
        }

        Console.WriteLine("Press any key to stop all participants...");
        Console.ReadKey();
    }
    finally
    {
        // Clean up all participants
        foreach (var participant in participants)
        {
            try
            {
                await participant.StopAsync();
                await participant.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping participant: {ex.Message}");
            }
        }
    }
}
```

## Integration with ASP.NET Core

### Registering as a Hosted Service

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register your dependencies
    services.AddSingleton<IJobProcessor, JobProcessor>();
    
    // Register the leader-elected background service
    services.AddHostedService<LeaderElectedBackgroundService>();
}
```

### Using Dependency Injection

```csharp
public class LeaderElectedController : ControllerBase
{
    private readonly ILeaderElectionService _leaderService;

    public LeaderElectedController()
    {
        // In a real application, you might inject this or use a factory
        _leaderService = new InProcessLeaderElectionService(
            "api-leader",
            Environment.MachineName);
    }

    [HttpGet("status")]
    public IActionResult GetLeadershipStatus()
    {
        return Ok(new
        {
            IsLeader = _leaderService.IsLeader,
            ParticipantId = _leaderService.ParticipantId,
            ElectionName = _leaderService.ElectionName,
            CurrentLeader = _leaderService.CurrentLeader?.ParticipantId
        });
    }

    [HttpPost("acquire")]
    public async Task<IActionResult> TryAcquireLeadershipAsync()
    {
        bool success = await _leaderService.TryAcquireLeadershipAsync();
        return Ok(new { Success = success, IsLeader = _leaderService.IsLeader });
    }

    [HttpPost("release")]
    public async Task<IActionResult> ReleaseLeadershipAsync()
    {
        await _leaderService.ReleaseLeadershipAsync();
        return Ok(new { IsLeader = _leaderService.IsLeader });
    }
}
```

## Common Mistakes to Avoid

### 1. Not Handling Leadership Loss

```csharp
// ❌ Wrong - doesn't handle leadership loss
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        StartCriticalProcess();
    }
    // Missing: what happens when leadership is lost?
};

// ✅ Correct - handles both gain and loss
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        StartCriticalProcess();
    }
    else if (args.LeadershipLost)
    {
        StopCriticalProcess(); // Important!
    }
};
```

### 2. Relying Only on Event Arguments for Leader Information

```csharp
// ❌ Wrong - relies on potentially incomplete event data
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        // args.CurrentLeader might be null in error scenarios
        var leaderId = args.CurrentLeader.ParticipantId; // Potential NullReferenceException!
        Console.WriteLine($"New leader: {leaderId}");
    }
};

// ✅ Correct - uses service properties for reliable information
service.LeadershipChanged += (sender, args) =>
{
    if (args.LeadershipGained)
    {
        // Always use service properties for reliable information
        var currentLeader = service.CurrentLeader;
        if (currentLeader != null)
        {
            Console.WriteLine($"New leader: {currentLeader.ParticipantId}");
        }
        else
        {
            Console.WriteLine("Leadership gained but leader info unavailable");
        }
    }
};
```

### 3. Not Disposing Services

```csharp
// ❌ Wrong - memory leak
var service = new InProcessLeaderElectionService("election", "participant");
// Service is never disposed

// ✅ Correct - proper disposal
await using var service = new InProcessLeaderElectionService("election", "participant");
// Service is automatically disposed
```

### 4. Ignoring Leadership Status in Operations

```csharp
// ❌ Wrong - doesn't check leadership status
private async void ProcessJobs(object? state)
{
    var jobs = await GetJobsAsync();
    foreach (var job in jobs)
    {
        await ProcessJobAsync(job); // Might not be leader anymore!
    }
}

// ✅ Correct - checks leadership status
private async void ProcessJobs(object? state)
{
    if (!_leaderService.IsLeader) return;
    
    var jobs = await GetJobsAsync();
    foreach (var job in jobs)
    {
        if (!_leaderService.IsLeader) break; // Check before each job
        await ProcessJobAsync(job);
    }
}
```

## Choosing Between In-Process and Redis

### Use In-Process When:
- ✅ Multiple services/threads within the **same application**
- ✅ Simple coordination needs
- ✅ No external dependencies preferred
- ✅ Maximum performance (no network overhead)
- ✅ Development and testing scenarios

```csharp
// Example: Multiple background services in one ASP.NET Core app
var jobProcessor = new InProcessLeaderElectionService("jobs", "processor-1");
var cacheWarmer = new InProcessLeaderElectionService("cache", "warmer-1");
var healthChecker = new InProcessLeaderElectionService("health", "checker-1");
```

### Use Redis-Based When:
- ✅ Multiple **applications across different machines**
- ✅ Microservices coordination
- ✅ High availability requirements
- ✅ Cross-datacenter scenarios
- ✅ Existing Redis infrastructure

```csharp
// Example: Multiple microservices coordinating globally
var redis = ConnectionMultiplexer.Connect("redis-cluster.company.com:6379");
var serializer = new ReflectionJsonDataSerializer();

// Service A on Machine 1
var serviceA = new RedisLeaderElectionService(redis, serializer, "global-processor", "service-a-machine1");

// Service B on Machine 2  
var serviceB = new RedisLeaderElectionService(redis, serializer, "global-processor", "service-b-machine2");

// Only one will be leader across the entire distributed system
```

## Next Steps

Now that you understand the basics, explore these advanced topics:

- **[Advanced Configuration](leader-election-advanced.md)**: Learn about custom lease stores and advanced timing configurations
- **[Troubleshooting](leader-election-troubleshooting.md)**: Common issues and how to resolve them
- **[Performance Tuning](leader-election-advanced.md#performance-tuning)**: Optimize for your specific use case

## Summary

In this guide, you learned how to:

1. ✅ Set up basic leader election
2. ✅ Handle leadership changes with events
3. ✅ Use automatic vs manual leadership management
4. ✅ Implement common patterns for background services
5. ✅ Work with multiple participants
6. ✅ Integrate with ASP.NET Core applications
7. ✅ Avoid common mistakes

The leader election utilities provide a robust foundation for building distributed systems that require coordination and single-leader semantics. Start with the simple patterns and gradually adopt more advanced features as your needs grow.
