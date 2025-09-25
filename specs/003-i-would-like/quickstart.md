# Quickstart: Version-Aware ConsistentHashing.HashRing

**Date**: 2025-09-25
**Feature**: Version-Aware ConsistentHashing.HashRing Extension
**Estimated Time**: 5 minutes

## Overview

This quickstart demonstrates how to use the version-aware HashRing for data migration scenarios. You'll learn how to enable history, create snapshots, and query servers across configurations.

## Prerequisites

- .NET 9.0 SDK
- AdaptArch.Common.Utilities NuGet package (with version-aware features)
- Basic understanding of consistent hashing concepts

## Step 1: Enable Version-Aware Features

```csharp
using AdaptArch.Common.Utilities.ConsistentHashing;

// Create hash ring with version history enabled
var options = new HashRingOptions
{
    EnableVersionHistory = true,
    MaxHistorySize = 3,
    DefaultVirtualNodes = 42
};

var hashRing = new HashRing<string>(options);
```

## Step 2: Set Up Initial Configuration

```csharp
// Add initial servers (e.g., for database sharding)
hashRing.Add("server-1");
hashRing.Add("server-2");

// Test key lookup with initial configuration
var testKey = System.Text.Encoding.UTF8.GetBytes("user:12345");
var primaryServer = hashRing.GetServer(testKey);
Console.WriteLine($"Primary server: {primaryServer}");
// Expected output: "Primary server: server-1" or "server-2"

// Verify no history exists yet
Console.WriteLine($"History enabled: {hashRing.IsVersionHistoryEnabled}");
Console.WriteLine($"History count: {hashRing.HistoryCount}");
// Expected output: "History enabled: True", "History count: 0"
```

## Step 3: Create Configuration Snapshot

```csharp
// Create snapshot of current configuration before adding new server
hashRing.CreateConfigurationSnapshot();

// Add new server (simulating scale-out scenario)
hashRing.Add("server-3");

Console.WriteLine($"History count after snapshot: {hashRing.HistoryCount}");
// Expected output: "History count after snapshot: 1"
```

## Step 4: Query Server Candidates During Migration

```csharp
// Get server candidates for data migration
var candidates = hashRing.GetServerCandidates(testKey);

Console.WriteLine($"Current server: {candidates.Servers[0]}");
Console.WriteLine($"Total candidates: {candidates.Servers.Count}");
Console.WriteLine($"Configurations consulted: {candidates.ConfigurationCount}");
Console.WriteLine($"Has history: {candidates.HasHistory}");

// List all candidate servers
foreach (var server in candidates.Servers)
{
    Console.WriteLine($"  Candidate: {server}");
}

// Expected output:
// Current server: server-X (current config result)
// Total candidates: 2 (assuming different servers)
// Configurations consulted: 2
// Has history: True
// Candidate: server-X
// Candidate: server-Y
```

## Step 5: Handle Multiple Snapshots

```csharp
// Create additional snapshots to test history management
hashRing.CreateConfigurationSnapshot();
hashRing.Add("server-4");

hashRing.CreateConfigurationSnapshot();
hashRing.Add("server-5");

Console.WriteLine($"History count: {hashRing.HistoryCount}");
// Expected output: "History count: 3" (at maximum)

// Test history limit enforcement
try
{
    hashRing.CreateConfigurationSnapshot(); // Should exceed limit
}
catch (HashRingHistoryLimitExceededException ex)
{
    Console.WriteLine($"History limit reached: {ex.Message}");
    Console.WriteLine($"Max size: {ex.MaxHistorySize}, Current: {ex.CurrentCount}");
    // Expected output showing limit enforcement
}
```

## Step 6: Query with Maximum Candidates

```csharp
// Limit the number of server candidates returned
var limitedCandidates = hashRing.GetServerCandidates(testKey, maxCandidates: 2);

Console.WriteLine($"Limited candidates count: {limitedCandidates.Servers.Count}");
// Expected output: "Limited candidates count: 2"

// Test with TryGet pattern
if (hashRing.TryGetServerCandidates(testKey, out var result))
{
    Console.WriteLine($"Successfully found {result.Servers.Count} candidates");
}
```

## Step 7: Clear History After Migration

```csharp
// After external migration is complete, clear history
hashRing.ClearHistory();

Console.WriteLine($"History count after clear: {hashRing.HistoryCount}");
// Expected output: "History count after clear: 0"

// Verify behavior without history
var candidatesNoHistory = hashRing.GetServerCandidates(testKey);
Console.WriteLine($"Candidates without history: {candidatesNoHistory.Servers.Count}");
Console.WriteLine($"Has history: {candidatesNoHistory.HasHistory}");
// Expected output: "Candidates without history: 1", "Has history: False"
```

## Step 8: Error Handling

```csharp
// Test edge cases
var emptyRing = new HashRing<string>(options);

try
{
    emptyRing.GetServerCandidates(testKey);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Empty ring error: {ex.Message}");
    // Expected: Error about no available servers
}

// Test with history disabled
var noHistoryOptions = new HashRingOptions { EnableVersionHistory = false };
var noHistoryRing = new HashRing<string>(noHistoryOptions);
noHistoryRing.Add("server-1");

try
{
    noHistoryRing.CreateConfigurationSnapshot();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"History disabled error: {ex.Message}");
    // Expected: Error about version history not being enabled
}
```

## Complete Example

```csharp
using AdaptArch.Common.Utilities.ConsistentHashing;

class Program
{
    static void Main()
    {
        // Setup
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 3
        };
        var ring = new HashRing<string>(options);

        // Initial configuration
        ring.Add("db-server-1");
        ring.Add("db-server-2");

        // Migration scenario
        var userKey = System.Text.Encoding.UTF8.GetBytes("user:alice");
        var initialServer = ring.GetServer(userKey);

        ring.CreateConfigurationSnapshot();
        ring.Add("db-server-3");

        var migrationCandidates = ring.GetServerCandidates(userKey);

        // Data migration logic would use both servers:
        // 1. Read from migrationCandidates.Servers[1] (historical)
        // 2. Write to migrationCandidates.Servers[0] (current)

        ring.ClearHistory(); // After migration complete

        Console.WriteLine($"Migration completed. Final server: {ring.GetServer(userKey)}");
    }
}
```

## Next Steps

1. **Integration**: Integrate with your data migration pipeline
2. **Monitoring**: Add logging for snapshot creation and history management
3. **Testing**: Create comprehensive tests for your specific migration scenarios
4. **Performance**: Benchmark with your expected data volume and server counts

## Troubleshooting

**Q: History limit exceeded exception**
A: Either increase `MaxHistorySize` or clear history more frequently during migration

**Q: Server candidates returning duplicates**
A: This shouldn't happen - the API automatically deduplicates. Check for custom equality implementation issues

**Q: Performance degradation with history enabled**
A: Each historical configuration adds O(log n) to lookup time. Consider smaller history sizes for performance-critical scenarios

**Q: Memory usage concerns**
A: Each snapshot stores O(servers × virtual_nodes) data. Monitor memory usage with large rings and adjust `MaxHistorySize` accordingly