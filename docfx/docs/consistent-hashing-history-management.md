# Consistent Hashing Snapshot History Management

The Consistent Hashing utility provides automatic snapshot history management that tracks configuration changes over time. Snapshot history is always enabled and provides the foundation for all key lookups in the HashRing.

## Overview

Snapshot history provides:

- **Immutable Configuration Snapshots**: All key lookups use immutable snapshots, not the current ring state
- **Automatic FIFO Management**: By default, oldest snapshots are automatically removed when the limit is reached
- **Migration Support**: Track configuration changes during cluster topology changes
- **Configurable Limits**: Control memory usage with `MaxHistorySize` (default: 3)
- **Flexible Behavior**: Choose between FIFO (default) or explicit error handling

## Understanding Snapshot-Based Lookups

**Important**: All `GetServer()` and `GetServers()` calls use configuration snapshots, not the current ring state. You must call `CreateConfigurationSnapshot()` after adding/removing servers and before lookup operations.

```csharp
var ring = new HashRing<string>();

ring.Add("server-1");
ring.Add("server-2");

// At this point, GetServer() will fail - no snapshots exist yet

ring.CreateConfigurationSnapshot();  // Create snapshot to enable lookups

// Now GetServer() will work using the snapshot
var server = ring.GetServer("key-123");
```

## History Limit Behaviors

### Default: FIFO (RemoveOldest)

By default, HashRing uses FIFO behavior - oldest snapshots are automatically removed when the limit is reached:

```csharp
// Default options: MaxHistorySize = 3, HistoryLimitBehavior = RemoveOldest
var ring = new HashRing<string>();

ring.Add("server-1");
ring.CreateConfigurationSnapshot();  // Snapshot 1

ring.Add("server-2");
ring.CreateConfigurationSnapshot();  // Snapshot 2

ring.Add("server-3");
ring.CreateConfigurationSnapshot();  // Snapshot 3 - history full (3/3)

ring.Add("server-4");
ring.CreateConfigurationSnapshot();  // Snapshot 4 - Snapshot 1 automatically removed

// Continues indefinitely - always room for new snapshots
Console.WriteLine($"History: {ring.HistoryCount}/{ring.MaxHistorySize}");  // 3/3
```

### Explicit Control: ThrowError

For explicit control over history management, use `HistoryLimitBehavior.ThrowError`:

```csharp
var options = new HashRingOptions
{
    MaxHistorySize = 3,
    HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
};
var ring = new HashRing<string>(options);

// Fill history to limit
ring.Add("server-1");
ring.CreateConfigurationSnapshot();
ring.Add("server-2");
ring.CreateConfigurationSnapshot();
ring.Add("server-3");
ring.CreateConfigurationSnapshot();

// Now at limit - next snapshot throws
try
{
    ring.CreateConfigurationSnapshot();
}
catch (HashRingHistoryLimitExceededException ex)
{
    Console.WriteLine($"History limit reached!");
    Console.WriteLine($"Max Size: {ex.MaxHistorySize}");
    Console.WriteLine($"Current Count: {ex.CurrentCount}");

    // Handle the limit - see management strategies below
}
```

### Checking History Status

```csharp
// Monitor current usage
Console.WriteLine($"History Count: {ring.HistoryCount}");
Console.WriteLine($"Max History Size: {ring.MaxHistorySize}");

// Calculate usage percentage
var usagePercent = (ring.HistoryCount * 100.0) / ring.MaxHistorySize;
Console.WriteLine($"History usage: {usagePercent:F1}%");
```

## Using Clear to Manage History Limits

### ClearHistory() Method

The most common approach is to clear the history when the limit is reached:

```csharp
public void ManageHistoryWithClear(HashRing<string> ring)
{
    try
    {
        // Try to create a new snapshot
        ring.CreateConfigurationSnapshot();
    }
    catch (HashRingHistoryLimitExceededException)
    {
        // Clear all history to make room for new snapshots
        ring.ClearHistory();

        // Now create the new snapshot
        ring.CreateConfigurationSnapshot();

        Console.WriteLine("History cleared and new snapshot created");
    }
}
```

### Proactive History Management

You can also manage history proactively before reaching the limit:

```csharp
public void ProactiveHistoryManagement(HashRing<string> ring)
{
    // Check if we're near the limit
    if (ring.HistoryCount >= ring.MaxHistorySize - 1)
    {
        Console.WriteLine("Approaching history limit, clearing history...");
        ring.ClearHistory();
    }

    // Safe to create new snapshot
    ring.CreateConfigurationSnapshot();
    Console.WriteLine($"New snapshot created. History count: {ring.HistoryCount}");
}
```

### Selective History Management

For more sophisticated scenarios, you might want to preserve some history:

```csharp
public void SelectiveHistoryManagement(HashRing<string> ring)
{
    try
    {
        ring.CreateConfigurationSnapshot();
    }
    catch (HashRingHistoryLimitExceededException)
    {
        // Option 1: Clear all history
        ring.ClearHistory();

        // Option 2: Could implement custom logic here
        // For example, you might want to:
        // - Export important snapshots before clearing
        // - Log the historical configurations
        // - Notify monitoring systems

        // Create the new snapshot after clearing
        ring.CreateConfigurationSnapshot();
    }
}
```

## Complete Example: Migration with History Management

Here's a comprehensive example showing history management during a server migration using ThrowError behavior:

```csharp
public class MigrationWithHistoryManagement
{
    private readonly HashRing<string> _ring;

    public MigrationWithHistoryManagement()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 5,  // Small limit for demonstration
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError  // Explicit control
        };
        _ring = new HashRing<string>(options);
    }

    public void PerformMigration()
    {
        // Initial setup
        _ring.Add("server-1");
        _ring.Add("server-2");
        _ring.CreateConfigurationSnapshot();  // Enable lookups
        Console.WriteLine("Initial configuration ready");

        // Simulate multiple migration steps
        for (int step = 1; step <= 8; step++)
        {
            Console.WriteLine($"\n--- Migration Step {step} ---");

            try
            {
                // Create snapshot before each change for history
                _ring.CreateConfigurationSnapshot();
                Console.WriteLine("Configuration snapshot created");

                // Make changes
                _ring.Add($"server-new-{step}");

                // Create snapshot after change to enable lookups
                _ring.CreateConfigurationSnapshot();
                Console.WriteLine($"Added server-new-{step}");

                // Show current status
                ShowStatus();
            }
            catch (HashRingHistoryLimitExceededException ex)
            {
                Console.WriteLine($"History limit reached: {ex.MaxHistorySize} snapshots");
                Console.WriteLine("Clearing history to continue migration...");

                // Clear history and continue
                _ring.ClearHistory();

                // Create snapshot and continue with changes
                _ring.Add($"server-new-{step}");
                _ring.CreateConfigurationSnapshot();

                Console.WriteLine("History cleared, migration step completed");
                ShowStatus();
            }
        }

        Console.WriteLine("\nMigration completed successfully!");
    }

    private void ShowStatus()
    {
        Console.WriteLine($"Current servers: {_ring.Servers.Count}");
        Console.WriteLine($"History count: {_ring.HistoryCount}/{_ring.MaxHistorySize}");

        // Test server resolution
        var server = _ring.GetServer("test-key");
        Console.WriteLine($"Test key routes to: {server}");

        // Get multiple candidates for redundancy
        var servers = _ring.GetServers("test-key", 3);
        Console.WriteLine($"Available replicas: {string.Join(", ", servers)}");
    }
}
```

## Best Practices

### 1. Monitor History Usage

```csharp
public void MonitorHistoryUsage(HashRing<string> ring)
{
    var usagePercent = (ring.HistoryCount * 100.0) / ring.MaxHistorySize;

    if (usagePercent > 80)
    {
        Console.WriteLine($"Warning: History usage at {usagePercent:F1}%");
    }

    Console.WriteLine($"History: {ring.HistoryCount}/{ring.MaxHistorySize} " +
                     $"({usagePercent:F1}%)");
}
```

### 2. Implement History Management Policies

```csharp
public enum HistoryPolicy
{
    ClearAll,
    ClearOldest,
    PreventNewSnapshots
}

public class HistoryManager
{
    private readonly HistoryPolicy _policy;

    public void HandleHistoryLimit(HashRing<string> ring)
    {
        switch (_policy)
        {
            case HistoryPolicy.ClearAll:
                ring.ClearHistory();
                break;

            case HistoryPolicy.PreventNewSnapshots:
                throw new InvalidOperationException("History limit reached, no new snapshots allowed");

            // Note: ClearOldest would require custom implementation
            // as the current API only supports clearing all history
        }
    }
}
```

### 3. Log History Operations

```csharp
public void CreateSnapshotWithLogging(HashRing<string> ring, ILogger logger)
{
    try
    {
        ring.CreateConfigurationSnapshot();
        logger.LogInformation("Configuration snapshot created. History: {Count}/{Max}",
            ring.HistoryCount, ring.MaxHistorySize);
    }
    catch (HashRingHistoryLimitExceededException ex)
    {
        logger.LogWarning("History limit reached. Clearing history. Max: {Max}, Current: {Current}",
            ex.MaxHistorySize, ex.CurrentCount);

        ring.ClearHistory();
        ring.CreateConfigurationSnapshot();

        logger.LogInformation("History cleared and new snapshot created");
    }
}
```

### 4. Testing History Limits

```csharp
[Fact]
public void TestHistoryLimitWithThrowError()
{
    var options = new HashRingOptions
    {
        MaxHistorySize = 2,
        HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
    };
    var ring = new HashRing<string>(options);

    ring.Add("server-1");

    // Fill history to limit
    ring.CreateConfigurationSnapshot();
    ring.Add("server-2");
    ring.CreateConfigurationSnapshot();

    Assert.Equal(2, ring.HistoryCount);

    // This should throw
    Assert.Throws<HashRingHistoryLimitExceededException>(() =>
        ring.CreateConfigurationSnapshot());

    // Clear and verify
    ring.ClearHistory();
    Assert.Equal(0, ring.HistoryCount);

    // Should work now
    ring.CreateConfigurationSnapshot();
    Assert.Equal(1, ring.HistoryCount);
}

[Fact]
public void TestHistoryLimitWithFIFO()
{
    var options = new HashRingOptions
    {
        MaxHistorySize = 2,
        HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest
    };
    var ring = new HashRing<string>(options);

    ring.Add("server-1");
    ring.CreateConfigurationSnapshot();  // Snapshot 1

    ring.Add("server-2");
    ring.CreateConfigurationSnapshot();  // Snapshot 2

    Assert.Equal(2, ring.HistoryCount);

    // This should automatically remove oldest (Snapshot 1)
    ring.Add("server-3");
    ring.CreateConfigurationSnapshot();  // Snapshot 3 - Snapshot 1 removed

    // Still at limit, but oldest was removed
    Assert.Equal(2, ring.HistoryCount);
}
```

## Common Scenarios

### Deployment Pipelines

```csharp
public class DeploymentPipeline
{
    public void DeployWithHistory(HashRing<string> ring, string[] newServers)
    {
        foreach (var server in newServers)
        {
            // Snapshot before each server addition
            TryCreateSnapshot(ring);
            ring.Add(server);
        }
    }

    private void TryCreateSnapshot(HashRing<string> ring)
    {
        try
        {
            ring.CreateConfigurationSnapshot();
        }
        catch (HashRingHistoryLimitExceededException)
        {
            // Clear history during deployments to ensure progress
            ring.ClearHistory();
            ring.CreateConfigurationSnapshot();
        }
    }
}
```

### Maintenance Operations

```csharp
public class MaintenanceOperations
{
    public void PerformMaintenance(HashRing<string> ring)
    {
        // Create snapshot before maintenance
        EnsureSnapshotCapacity(ring);
        ring.CreateConfigurationSnapshot();

        // Perform maintenance operations...
        // Add/remove servers as needed
    }

    private void EnsureSnapshotCapacity(HashRing<string> ring)
    {
        if (ring.HistoryCount >= ring.MaxHistorySize)
        {
            Console.WriteLine("Clearing history to make room for maintenance snapshot");
            ring.ClearHistory();
        }
    }
}
```

## Configuration Recommendations

### Small Clusters (< 10 servers)
```csharp
// Use default FIFO behavior for simplicity
var options = new HashRingOptions
{
    MaxHistorySize = 5  // 5 snapshots usually sufficient
    // HistoryLimitBehavior = RemoveOldest (default)
};
```

### Medium Clusters (10-50 servers)
```csharp
var options = new HashRingOptions
{
    MaxHistorySize = 10,  // More history for complex migrations
    HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest  // Auto-manage
};
```

### Large Clusters (50+ servers) with Explicit Control
```csharp
var options = new HashRingOptions
{
    MaxHistorySize = 20,  // Extensive history for large-scale operations
    HistoryLimitBehavior = HistoryLimitBehavior.ThrowError  // Explicit control
};
```

### Production Deployments with FIFO
```csharp
// Recommended for most production scenarios
var options = new HashRingOptions
{
    MaxHistorySize = 10,  // Balance between history and memory
    HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest  // No intervention needed
};
```

## Troubleshooting

### Snapshot Required Before Lookups
If `GetServer()` fails with an error about no snapshots:
```csharp
// Wrong - no snapshot created
var ring = new HashRing<string>();
ring.Add("server-1");
var server = ring.GetServer("key");  // Fails!

// Correct - snapshot created
var ring = new HashRing<string>();
ring.Add("server-1");
ring.CreateConfigurationSnapshot();  // Create snapshot first
var server = ring.GetServer("key");  // Works!
```

### FIFO vs ThrowError Behavior
Choose the right behavior for your use case:
```csharp
// FIFO (default) - Best for most cases, no manual intervention
var fifoRing = new HashRing<string>();  // Default MaxHistorySize=3, RemoveOldest

// ThrowError - Best when you need explicit control
var errorRing = new HashRing<string>(new HashRingOptions
{
    MaxHistorySize = 10,
    HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
});
```

### Memory Concerns
Monitor memory usage with large histories:
```csharp
// Each snapshot stores server list and virtual node mappings
// Estimate: ~100 bytes per server + ~32 bytes per virtual node
var estimatedBytes = ring.HistoryCount * (ring.Servers.Count * 100 + ring.VirtualNodeCount * 32);

// For FIFO behavior, memory is bounded by MaxHistorySize
var maxBytes = ring.MaxHistorySize * (ring.Servers.Count * 100 + ring.VirtualNodeCount * 32);
Console.WriteLine($"Maximum memory usage: ~{maxBytes / 1024}KB");
```

## Related Documentation

- [Consistent Hashing](consistent-hashing.md) - Main consistent hashing documentation
- [Extension Methods](extension-methods.md) - Extension method utilities
- [Synchronization Utilities](synchronization-utilities.md) - Thread-safe utilities