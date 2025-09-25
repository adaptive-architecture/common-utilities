# Consistent Hashing History Management

The Consistent Hashing utility provides advanced version history management capabilities that allow you to track configuration snapshots over time. This is particularly useful for migration scenarios, rollback capabilities, and maintaining consistency during cluster topology changes.

## Overview

Version history enables you to:

- **Track Configuration Changes**: Store snapshots of ring configurations over time
- **Handle Migration Scenarios**: Find servers from both current and historical configurations
- **Manage Resource Usage**: Control memory usage with configurable history limits
- **Create New Snapshots**: Reset history when needed using the Clear method

## Enabling Version History

Version history is disabled by default and must be explicitly enabled:

```csharp
var options = new HashRingOptions
{
    EnableVersionHistory = true,
    MaxHistorySize = 10  // Store up to 10 historical snapshots
};

var ring = new HashRing<string>(options);
```

## History Limit Management

### Understanding the Limit

The history manager enforces a strict limit on the number of snapshots that can be stored:

```csharp
// Check current status
Console.WriteLine($"History Count: {ring.HistoryCount}");
Console.WriteLine($"Max History Size: {ring.MaxHistorySize}");
Console.WriteLine($"Is Version History Enabled: {ring.IsVersionHistoryEnabled}");
```

### Reaching the History Limit

When you attempt to create a snapshot that would exceed the limit, a `HashRingHistoryLimitExceededException` is thrown:

```csharp
try
{
    ring.CreateConfigurationSnapshot();
}
catch (HashRingHistoryLimitExceededException ex)
{
    Console.WriteLine($"History limit reached!");
    Console.WriteLine($"Max Size: {ex.MaxHistorySize}");
    Console.WriteLine($"Current Count: {ex.CurrentCount}");

    // Handle the limit - see solutions below
}
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

Here's a comprehensive example showing history management during a server migration:

```csharp
public class MigrationWithHistoryManagement
{
    private readonly HashRing<string> _ring;

    public MigrationWithHistoryManagement()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 5  // Small limit for demonstration
        };
        _ring = new HashRing<string>(options);
    }

    public void PerformMigration()
    {
        // Initial setup
        _ring.Add("server-1");
        _ring.Add("server-2");
        Console.WriteLine("Initial configuration ready");

        // Simulate multiple migration steps
        for (int step = 1; step <= 8; step++)
        {
            Console.WriteLine($"\n--- Migration Step {step} ---");

            try
            {
                // Create snapshot before each change
                _ring.CreateConfigurationSnapshot();
                Console.WriteLine("Configuration snapshot created");

                // Make changes
                _ring.Add($"server-new-{step}");
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

                // Create new snapshot and continue with changes
                _ring.CreateConfigurationSnapshot();
                _ring.Add($"server-new-{step}");

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

        // Test server resolution with history
        var candidates = _ring.GetServerCandidates("test-key");
        Console.WriteLine($"Server candidates available: {candidates.Servers.Count}");
        Console.WriteLine($"Configurations checked: {candidates.ConfigurationCount}");
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
[Test]
public void TestHistoryLimitHandling()
{
    var options = new HashRingOptions
    {
        EnableVersionHistory = true,
        MaxHistorySize = 2
    };
    var ring = new HashRing<string>(options);

    ring.Add("server-1");

    // Fill history to limit
    ring.CreateConfigurationSnapshot();
    ring.Add("server-2");
    ring.CreateConfigurationSnapshot();
    ring.Add("server-3");

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
var options = new HashRingOptions
{
    EnableVersionHistory = true,
    MaxHistorySize = 5  // 5 snapshots usually sufficient
};
```

### Medium Clusters (10-50 servers)
```csharp
var options = new HashRingOptions
{
    EnableVersionHistory = true,
    MaxHistorySize = 10  // More history for complex migrations
};
```

### Large Clusters (50+ servers)
```csharp
var options = new HashRingOptions
{
    EnableVersionHistory = true,
    MaxHistorySize = 20  // Extensive history for large-scale operations
};
```

## Troubleshooting

### History Not Clearing
If `ClearHistory()` throws an exception:
```csharp
if (!ring.IsVersionHistoryEnabled)
{
    throw new InvalidOperationException("Version history is not enabled");
}
```

### Memory Concerns
Monitor memory usage with large histories:
```csharp
// Each snapshot stores server list and virtual node mappings
// Estimate: ~100 bytes per server + ~32 bytes per virtual node
var estimatedBytes = ring.HistoryCount * (ring.Servers.Count * 100 + ring.VirtualNodeCount * 32);
```

## Related Documentation

- [Consistent Hashing](consistent-hashing.md) - Main consistent hashing documentation
- [Extension Methods](extension-methods.md) - Extension method utilities
- [Synchronization Utilities](synchronization-utilities.md) - Thread-safe utilities