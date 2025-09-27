using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.Samples.ConsistentHashing;

/// <summary>
/// Example demonstrating how to use consistent hashing for database connection routing.
/// This helps distribute database operations across multiple database replicas or shards.
/// </summary>
public static class DatabaseRoutingExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== Database Routing with Consistent Hashing ===");

        // Setup: Create hash ring with database connection strings
        var dbRing = new HashRing<string>();

        // Add multiple database servers (could be read replicas, shards, etc.)
        dbRing.Add("db-primary.example.com:5432");
        dbRing.Add("db-replica-1.example.com:5432");
        dbRing.Add("db-replica-2.example.com:5432");
        dbRing.Add("db-shard-1.example.com:5432");

        Console.WriteLine($"Configured {dbRing.Servers.Count} database servers:");
        foreach (var server in dbRing.Servers)
        {
            Console.WriteLine($"  - {server}");
        }
        Console.WriteLine();

        // Simulate routing different user IDs to different databases
        var userIds = new[] { "user-12345", "user-67890", "user-11111", "user-22222", "user-33333" };

        Console.WriteLine("User ID to Database Server routing:");
        foreach (var userId in userIds)
        {
            var dbServer = dbRing.GetServer(userId);
            Console.WriteLine($"  {userId} -> {dbServer}");
        }
        Console.WriteLine();

        // Demonstrate consistency - same user ID always goes to same database
        Console.WriteLine("Consistency check (same user ID multiple times):");
        for (int i = 0; i < 5; i++)
        {
            var dbServer = dbRing.GetServer("user-12345");
            Console.WriteLine($"  Attempt {i + 1}: user-12345 -> {dbServer}");
        }
        Console.WriteLine();

        // Simulate database failure and failover
        Console.WriteLine("=== Simulating Database Failover ===");
        const string failedServer = "db-replica-1.example.com:5432";
        Console.WriteLine($"Removing failed server: {failedServer}");
        dbRing.Remove(failedServer);

        Console.WriteLine("Routing after failover:");
        foreach (var userId in userIds)
        {
            var dbServer = dbRing.GetServer(userId);
            Console.WriteLine($"  {userId} -> {dbServer}");
        }
        Console.WriteLine();

        // Simulate server recovery
        Console.WriteLine("=== Server Recovery ===");
        Console.WriteLine($"Adding recovered server back: {failedServer}");
        dbRing.Add(failedServer);

        Console.WriteLine("Routing after recovery:");
        foreach (var userId in userIds)
        {
            var dbServer = dbRing.GetServer(userId);
            Console.WriteLine($"  {userId} -> {dbServer}");
        }
        Console.WriteLine();

        // Advanced: Show distribution with many users
        Console.WriteLine("=== Load Distribution Analysis ===");
        var serverCounts = new Dictionary<string, int>();
        foreach (var server in dbRing.Servers)
        {
            serverCounts[server] = 0;
        }

        // Simulate routing 1000 users
        for (int i = 1; i <= 1000; i++)
        {
            var userId = $"user-{i:D5}";
            var dbServer = dbRing.GetServer(userId);
            serverCounts[dbServer]++;
        }

        Console.WriteLine("Distribution of 1000 users across servers:");
        foreach (var kvp in serverCounts)
        {
            var percentage = (kvp.Value * 100.0) / 1000;
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} users ({percentage:F1}%)");
        }

        Console.WriteLine("\n=== Database Routing Example Complete ===\n");
    }

    /// <summary>
    /// Example of a database routing service that uses consistent hashing.
    /// </summary>
    public class DatabaseRouter
    {
        private readonly HashRing<DatabaseConnection> _ring;

        public DatabaseRouter()
        {
            _ring = new HashRing<DatabaseConnection>();
        }

        public void AddDatabase(string connectionString, string name)
        {
            var connection = new DatabaseConnection(connectionString, name);
            _ring.Add(connection);
        }

        public void RemoveDatabase(string connectionString, string name)
        {
            var connection = new DatabaseConnection(connectionString, name);
            _ring.Remove(connection);
        }

        public DatabaseConnection GetDatabaseForUser(string userId)
        {
            if (String.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            return _ring.GetServer(userId);
        }

        public bool TryGetDatabaseForUser(string userId, out DatabaseConnection? database)
        {
            database = default;

            if (String.IsNullOrEmpty(userId))
                return false;

            return _ring.TryGetServer(userId, out database);
        }
    }

    /// <summary>
    /// Represents a database connection configuration.
    /// </summary>
    public record DatabaseConnection(string ConnectionString, string Name) : IEquatable<DatabaseConnection>
    {
        public override string ToString() => $"{Name} ({ConnectionString})";
    }
}
