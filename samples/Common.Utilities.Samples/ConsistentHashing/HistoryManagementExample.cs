using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.Samples.ConsistentHashing;

/// <summary>
/// Example demonstrating how to use the Clear method to manage history limits
/// in consistent hashing with version history enabled.
/// </summary>
public static class HistoryManagementExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== Consistent Hashing History Management Example ===\n");

        // Demonstrate different approaches to managing history limits
        DemonstrateBasicHistoryLimit();
        DemonstrateProactiveHistoryManagement();
        DemonstrateMigrationWithHistoryManagement();
        DemonstrateHistoryPolicies();

        Console.WriteLine("=== History Management Example Complete ===\n");
    }

    /// <summary>
    /// Shows what happens when you reach the history limit and how to handle it.
    /// </summary>
    private static void DemonstrateBasicHistoryLimit()
    {
        Console.WriteLine("--- Basic History Limit Handling ---");

        // Create a hash ring with a small history limit for demonstration
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 3
        };
        var ring = new HashRing<string>(options);

        // Add initial servers
        ring.Add("server-1");
        ring.Add("server-2");

        Console.WriteLine($"Initial setup - History: {ring.HistoryCount}/{ring.MaxHistorySize}");

        // Fill up the history
        for (int i = 1; i <= 3; i++)
        {
            ring.CreateConfigurationSnapshot();
            ring.Add($"server-{i + 2}");
            Console.WriteLine($"Added server-{i + 2} - History: {ring.HistoryCount}/{ring.MaxHistorySize}");
        }

        // Now we're at the limit - next snapshot will fail
        Console.WriteLine("\nAttempting to create snapshot beyond limit...");
        try
        {
            ring.CreateConfigurationSnapshot();
            Console.WriteLine("Snapshot created successfully");
        }
        catch (HashRingHistoryLimitExceededException ex)
        {
            Console.WriteLine($"❌ History limit exceeded! Max: {ex.MaxHistorySize}, Current: {ex.CurrentCount}");
            Console.WriteLine("🔧 Clearing history and retrying...");

            // Clear history and create new snapshot
            ring.ClearHistory();
            ring.CreateConfigurationSnapshot();

            Console.WriteLine($"✅ History cleared and new snapshot created - History: {ring.HistoryCount}/{ring.MaxHistorySize}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates proactive history management to avoid hitting limits.
    /// </summary>
    private static void DemonstrateProactiveHistoryManagement()
    {
        Console.WriteLine("--- Proactive History Management ---");

        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 5
        };
        var ring = new HashRing<string>(options);

        ring.Add("primary-server");

        // Simulate a deployment with many servers
        for (int i = 1; i <= 10; i++)
        {
            // Check if we're approaching the limit
            if (ring.HistoryCount >= ring.MaxHistorySize - 1)
            {
                Console.WriteLine($"📊 Approaching limit ({ring.HistoryCount}/{ring.MaxHistorySize}), clearing history...");
                ring.ClearHistory();
            }

            // Create snapshot and add server
            ring.CreateConfigurationSnapshot();
            ring.Add($"deploy-server-{i}");

            Console.WriteLine($"Added deploy-server-{i} - History: {ring.HistoryCount}/{ring.MaxHistorySize}");
        }

        Console.WriteLine($"✅ Deployment completed with {ring.Servers.Count} servers");
        Console.WriteLine();
    }

    /// <summary>
    /// Shows a realistic migration scenario with history management.
    /// </summary>
    private static void DemonstrateMigrationWithHistoryManagement()
    {
        Console.WriteLine("--- Migration Scenario with History Management ---");

        var migrationManager = new MigrationManager();
        migrationManager.PerformMigration();

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates different policies for handling history limits.
    /// </summary>
    private static void DemonstrateHistoryPolicies()
    {
        Console.WriteLine("--- Different History Management Policies ---");

        // Policy 1: Clear all history
        Console.WriteLine("\n1. Clear All History Policy:");
        var clearAllManager = new HistoryManager(HistoryPolicy.ClearAll);
        clearAllManager.DemonstratePolicy();

        // Policy 2: Monitor and warn
        Console.WriteLine("\n2. Monitor and Warn Policy:");
        var monitorManager = new HistoryManager(HistoryPolicy.MonitorAndWarn);
        monitorManager.DemonstratePolicy();

        // Policy 3: Fail fast
        Console.WriteLine("\n3. Fail Fast Policy:");
        var failFastManager = new HistoryManager(HistoryPolicy.FailFast);
        failFastManager.DemonstratePolicy();
    }
}

/// <summary>
/// Example migration manager that handles history limits during migrations.
/// </summary>
public class MigrationManager
{
    private readonly HashRing<string> _ring;

    public MigrationManager()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 4
        };
        _ring = new HashRing<string>(options);
    }

    public void PerformMigration()
    {
        // Initial cluster setup
        Console.WriteLine("🚀 Starting migration from legacy cluster...");
        _ring.Add("legacy-server-1");
        _ring.Add("legacy-server-2");

        var testKey = "user-12345";
        var originalServer = _ring.GetServer(testKey);
        Console.WriteLine($"📍 Test user routes to: {originalServer}");

        // Migration steps
        var migrationSteps = new[]
        {
            ("new-server-1", "Adding first new generation server"),
            ("new-server-2", "Adding second new generation server"),
            ("new-server-3", "Adding third new generation server"),
            ("legacy-server-3", "Adding temporary bridge server"),
            ("new-server-4", "Adding fourth new generation server"),
            ("new-server-5", "Adding final new generation server")
        };

        foreach (var (server, description) in migrationSteps)
        {
            Console.WriteLine($"\n📦 {description}");

            try
            {
                // Create snapshot before each migration step
                _ring.CreateConfigurationSnapshot();
                _ring.Add(server);

                Console.WriteLine($"✅ Added {server}");
                ShowMigrationStatus(testKey);
            }
            catch (HashRingHistoryLimitExceededException)
            {
                Console.WriteLine("⚠️ History limit reached during migration");
                Console.WriteLine("🔧 Clearing history to continue migration...");

                _ring.ClearHistory();
                _ring.CreateConfigurationSnapshot();
                _ring.Add(server);

                Console.WriteLine($"✅ History cleared, added {server}");
                ShowMigrationStatus(testKey);
            }
        }

        // Test server candidates with history
        Console.WriteLine("\n🔍 Testing server resolution with history:");
        var candidates = _ring.GetServerCandidates(System.Text.Encoding.UTF8.GetBytes(testKey));
        Console.WriteLine($"Primary server: {candidates.Servers.First()}");
        if (candidates.Servers.Count > 1)
        {
            Console.WriteLine($"Fallback servers: {string.Join(", ", candidates.Servers.Skip(1))}");
        }
        Console.WriteLine($"Total configurations checked: {candidates.ConfigurationCount}");

        Console.WriteLine("\n✅ Migration completed successfully!");
    }

    private void ShowMigrationStatus(string testKey)
    {
        var currentServer = _ring.GetServer(testKey);
        Console.WriteLine($"   📊 Status: {_ring.Servers.Count} servers, history {_ring.HistoryCount}/{_ring.MaxHistorySize}");
        Console.WriteLine($"   📍 Test user now routes to: {currentServer}");
    }
}

/// <summary>
/// Demonstrates different policies for handling history limits.
/// </summary>
public class HistoryManager
{
    private readonly HistoryPolicy _policy;
    private readonly HashRing<string> _ring;

    public HistoryManager(HistoryPolicy policy)
    {
        _policy = policy;
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 2  // Small limit for demonstration
        };
        _ring = new HashRing<string>(options);
    }

    public void DemonstratePolicy()
    {
        _ring.Add("server-1");

        // Fill history to limit
        _ring.CreateConfigurationSnapshot();
        _ring.Add("server-2");
        _ring.CreateConfigurationSnapshot();
        _ring.Add("server-3");

        Console.WriteLine($"Current status: {_ring.HistoryCount}/{_ring.MaxHistorySize} history slots used");

        // Try to create another snapshot
        try
        {
            HandleSnapshotCreation();
            Console.WriteLine($"✅ Policy handled successfully - History: {_ring.HistoryCount}/{_ring.MaxHistorySize}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Policy failed: {ex.Message}");
        }
    }

    private void HandleSnapshotCreation()
    {
        switch (_policy)
        {
            case HistoryPolicy.ClearAll:
                TryCreateSnapshotWithClearAll();
                break;

            case HistoryPolicy.MonitorAndWarn:
                TryCreateSnapshotWithMonitoring();
                break;

            case HistoryPolicy.FailFast:
                TryCreateSnapshotWithFailFast();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void TryCreateSnapshotWithClearAll()
    {
        try
        {
            _ring.CreateConfigurationSnapshot();
        }
        catch (HashRingHistoryLimitExceededException)
        {
            Console.WriteLine("🔧 Clearing all history automatically...");
            _ring.ClearHistory();
            _ring.CreateConfigurationSnapshot();
        }
    }

    private void TryCreateSnapshotWithMonitoring()
    {
        // Check usage before creating
        var usagePercent = (_ring.HistoryCount * 100.0) / _ring.MaxHistorySize;

        if (usagePercent >= 80)
        {
            Console.WriteLine($"⚠️ Warning: History usage at {usagePercent:F0}%");
        }

        if (_ring.HistoryCount >= _ring.MaxHistorySize)
        {
            Console.WriteLine("🔧 History full, clearing before creating new snapshot...");
            _ring.ClearHistory();
        }

        _ring.CreateConfigurationSnapshot();
    }

    private void TryCreateSnapshotWithFailFast()
    {
        if (_ring.HistoryCount >= _ring.MaxHistorySize)
        {
            throw new InvalidOperationException("History limit reached. Manual intervention required.");
        }

        _ring.CreateConfigurationSnapshot();
    }
}

/// <summary>
/// Different policies for handling history limits.
/// </summary>
public enum HistoryPolicy
{
    /// <summary>
    /// Automatically clear all history when limit is reached.
    /// </summary>
    ClearAll,

    /// <summary>
    /// Monitor usage and warn, clear proactively when needed.
    /// </summary>
    MonitorAndWarn,

    /// <summary>
    /// Fail fast when limit is reached, requiring manual intervention.
    /// </summary>
    FailFast
}

/// <summary>
/// Production-ready service that manages consistent hashing with history.
/// </summary>
public class ProductionHashRingService
{
    private readonly HashRing<string> _ring;
    private readonly ILogger _logger;
    private readonly HistoryManagementOptions _options;

    public ProductionHashRingService(ILogger logger, HistoryManagementOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _options = options ?? new HistoryManagementOptions();

        var ringOptions = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = _options.MaxHistorySize
        };
        _ring = new HashRing<string>(ringOptions);
    }

    public void CreateSnapshotSafely(string reason)
    {
        try
        {
            // Check if we need to clear history proactively
            if (ShouldClearHistory())
            {
                _logger.LogWarning("History usage at {Usage:F1}%, clearing history",
                    GetHistoryUsagePercent());
                ClearHistoryWithLogging();
            }

            _ring.CreateConfigurationSnapshot();
            _logger.LogInformation("Configuration snapshot created. Reason: {Reason}. History: {Count}/{Max}",
                reason, _ring.HistoryCount, _ring.MaxHistorySize);
        }
        catch (HashRingHistoryLimitExceededException ex)
        {
            _logger.LogError("Failed to create snapshot, history limit exceeded: {Max}/{Current}",
                ex.MaxHistorySize, ex.CurrentCount);

            if (_options.AutoClearOnLimit)
            {
                _logger.LogWarning("Auto-clearing history due to limit");
                ClearHistoryWithLogging();
                _ring.CreateConfigurationSnapshot();
                _logger.LogInformation("Snapshot created after clearing history");
            }
            else
            {
                throw;
            }
        }
    }

    public void AddServer(string server, string reason)
    {
        CreateSnapshotSafely($"Before adding server {server}: {reason}");
        _ring.Add(server);
        _logger.LogInformation("Server {Server} added to hash ring", server);
    }

    public void RemoveServer(string server, string reason)
    {
        CreateSnapshotSafely($"Before removing server {server}: {reason}");
        var removed = _ring.Remove(server);
        if (removed)
        {
            _logger.LogInformation("Server {Server} removed from hash ring", server);
        }
        else
        {
            _logger.LogWarning("Attempted to remove non-existent server {Server}", server);
        }
    }

    private bool ShouldClearHistory()
    {
        var usagePercent = GetHistoryUsagePercent();
        return usagePercent >= _options.ClearThresholdPercent;
    }

    private double GetHistoryUsagePercent()
    {
        return (_ring.HistoryCount * 100.0) / _ring.MaxHistorySize;
    }

    private void ClearHistoryWithLogging()
    {
        var oldCount = _ring.HistoryCount;
        _ring.ClearHistory();
        _logger.LogInformation("Cleared {Count} historical configurations", oldCount);
    }

    public ServerCandidateResult<string> GetServerCandidates(string key)
    {
        return _ring.GetServerCandidates(System.Text.Encoding.UTF8.GetBytes(key));
    }
}

/// <summary>
/// Configuration options for history management.
/// </summary>
public class HistoryManagementOptions
{
    /// <summary>
    /// Maximum number of historical snapshots to store.
    /// </summary>
    public int MaxHistorySize { get; set; } = 10;

    /// <summary>
    /// Percentage threshold at which to proactively clear history.
    /// </summary>
    public double ClearThresholdPercent { get; set; } = 80.0;

    /// <summary>
    /// Whether to automatically clear history when the limit is exceeded.
    /// </summary>
    public bool AutoClearOnLimit { get; set; } = true;
}

/// <summary>
/// Simple logger interface for demonstration.
/// </summary>
public interface ILogger
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
}

/// <summary>
/// Console-based logger implementation for demonstration.
/// </summary>
public class ConsoleLogger : ILogger
{
    public void LogInformation(string message, params object[] args)
    {
        Console.WriteLine($"[INFO] {string.Format(message, args)}");
    }

    public void LogWarning(string message, params object[] args)
    {
        Console.WriteLine($"[WARN] {string.Format(message, args)}");
    }

    public void LogError(string message, params object[] args)
    {
        Console.WriteLine($"[ERROR] {string.Format(message, args)}");
    }
}