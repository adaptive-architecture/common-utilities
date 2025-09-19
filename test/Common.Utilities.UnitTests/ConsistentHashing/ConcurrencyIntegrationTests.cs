using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class ConcurrencyIntegrationTests
{
    [Fact]
    public async Task HashRing_ConcurrentReads_ReturnConsistentResults()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        var testKeys = Enumerable.Range(1, 100).Select(i => $"key{i}").ToArray();
        var expectedMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        var results = new ConcurrentDictionary<string, ConcurrentBag<string>>();
        foreach (var key in testKeys)
        {
            results[key] = [];
        }

        // Act - Perform concurrent reads
        var tasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            await Task.Yield(); // Ensure actual async execution

            foreach (var key in testKeys)
            {
                var server = ring.GetServer(key);
                results[key].Add(server);
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert - All concurrent reads should return the same server for each key
        foreach (var key in testKeys)
        {
            var distinctResults = results[key].Distinct().ToArray();
            Assert.Single(distinctResults);
            Assert.Equal(expectedMapping[key], distinctResults[0]);
        }
    }

    [Fact]
    public async Task HashRing_ConcurrentReadsAndWrites_MaintainConsistency()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        var readResults = new ConcurrentBag<(string key, string server)>();
        var testKeys = Enumerable.Range(1, 50).Select(i => $"key{i}").ToArray();

        // Act - Mix concurrent reads with occasional writes
        var readTasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            await Task.Delay(Random.Shared.Next(1, 10)); // Random delay

            foreach (var key in testKeys.Take(10)) // Read subset of keys
            {
                try
                {
                    var server = ring.GetServer(key);
                    readResults.Add((key, server));
                }
                catch (InvalidOperationException)
                {
                    // Ring might be empty during server removal - this is acceptable
                }
            }
        }).ToArray();

        var writeTasks = new[]
        {
            Task.Run(async () =>
            {
                await Task.Delay(25);
                ring.Add("server3");
            }, TestContext.Current.CancellationToken),
            Task.Run(async () =>
            {
                await Task.Delay(50);
                ring.Add("server4");
            }, TestContext.Current.CancellationToken),
            Task.Run(async () =>
            {
                await Task.Delay(75);
                ring.Remove("server1");
            }, TestContext.Current.CancellationToken)
        };

        await Task.WhenAll(readTasks.Concat(writeTasks).ToArray());

        // Assert - All read results should be valid (mapped to servers that existed at read time)
        var allResults = readResults.ToArray();
        Assert.NotEmpty(allResults);

        // Group results by key and verify consistency within reasonable bounds
        foreach (var keyResults in (Dictionary<string, (string key, string server)[]>)allResults.GroupBy(r => r.key).ToDictionary(g => g.Key, g => g.ToArray()))
        {
            // Each key should have been mapped to valid servers
            Assert.All(keyResults.Value, result => Assert.NotNull(result.server));

            // Due to concurrent modifications, we might see different servers for the same key,
            // but the number of distinct servers should be reasonable (not too many)
            var distinctServers = keyResults.Value.Select(r => r.server).Distinct().ToArray();
            Assert.True(distinctServers.Length <= 4, // At most 4 different servers over time
                $"Key {keyResults.Key} mapped to too many different servers: {String.Join(", ", distinctServers)}");
        }
    }

    [Fact]
    public async Task HashRing_ConcurrentServerAdditions_AllServersRegistered()
    {
        // Arrange
        var ring = new HashRing<string>();
        const int serverCount = 20;
        var additionResults = new ConcurrentBag<bool>();

        // Act - Add servers concurrently
        var tasks = Enumerable.Range(1, serverCount).Select(async i =>
        {
            await Task.Delay(Random.Shared.Next(1, 50)); // Random delay

            var serverName = $"server{i}";
            ring.Add(serverName);

            // Verify server was added
            var contains = ring.Contains(serverName);
            additionResults.Add(contains);
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert - All servers should be successfully added
        Assert.All(additionResults, result => Assert.True(result));
        Assert.Equal(serverCount, ring.Servers.Count);

        // Verify all expected servers are present
        for (int i = 1; i <= serverCount; i++)
        {
            Assert.Contains($"server{i}", ring.Servers);
        }
    }

    [Fact]
    public async Task HashRing_ConcurrentServerRemovals_AllServersRemoved()
    {
        // Arrange
        var ring = new HashRing<string>();
        const int serverCount = 15;

        // Add servers first
        for (int i = 1; i <= serverCount; i++)
        {
            ring.Add($"server{i}");
        }

        var removalResults = new ConcurrentBag<bool>();

        // Act - Remove servers concurrently
        var tasks = Enumerable.Range(1, serverCount).Select(async i =>
        {
            await Task.Delay(Random.Shared.Next(1, 30)); // Random delay

            var serverName = $"server{i}";
            var removed = ring.Remove(serverName);
            removalResults.Add(removed);
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert - All servers should be successfully removed
        Assert.All(removalResults, result => Assert.True(result));
        Assert.Empty(ring.Servers);
        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
    }

    [Fact]
    public async Task HashRing_ConcurrentMixedOperations_MaintainsIntegrity()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        var operationResults = new ConcurrentBag<string>();
        const string testKey = "test_key";

        // Act - Mix different operations concurrently
        var tasks = new List<Task>();

        // Add read operations
        for (int i = 0; i < 30; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(1, 100));
                try
                {
                    var server = ring.GetServer(testKey);
                    operationResults.Add($"READ:{server}");
                }
                catch (InvalidOperationException)
                {
                    operationResults.Add("READ:EMPTY");
                }
            }, TestContext.Current.CancellationToken));
        }

        // Add server addition operations
        for (int i = 3; i <= 7; i++)
        {
            int serverNum = i; // Capture loop variable
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(10, 80));
                ring.Add($"server{serverNum}");
                operationResults.Add($"ADD:server{serverNum}");
            }, TestContext.Current.CancellationToken));
        }

        // Add server removal operations
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(150);
            bool removed = ring.Remove("server1");
            operationResults.Add($"REMOVE:server1:{removed}");
        }, TestContext.Current.CancellationToken));

        // Add contains checks
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(1, 120));
                bool contains = ring.Contains("server2");
                operationResults.Add($"CONTAINS:server2:{contains}");
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        // Assert - Operations completed without throwing exceptions
        var results = operationResults.ToArray();
        Assert.NotEmpty(results);

        // Verify final state integrity
        Assert.NotEmpty(ring.Servers); // Should have some servers remaining

        // All remaining servers should be valid
        foreach (var server in ring.Servers)
        {
            Assert.NotNull(server);
            Assert.True(ring.Contains(server));
        }

        // Server count should match virtual node distribution
        Assert.True(ring.VirtualNodeCount >= ring.Servers.Count);
    }

    [Fact]
    public async Task HashRing_ConcurrentTryGetServer_HandlesEmptyRingSafely()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        var results = new ConcurrentBag<(bool success, string server)>();
        const string testKey = "test_key";

        // Act - Concurrent TryGetServer calls while removing the only server
        var readTasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            await Task.Delay(Random.Shared.Next(1, 100));

            bool success = ring.TryGetServer(testKey, out string server);
            results.Add((success, server));
        }).ToArray();

        var removeTask = Task.Run(async () =>
        {
            await Task.Delay(25); // Remove server partway through reads
            ring.Remove("server1");
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(readTasks.Concat(new[] { removeTask }).ToArray());

        // Assert - All operations completed successfully
        var resultArray = results.ToArray();
        Assert.NotEmpty(resultArray);

        // Some calls should succeed (before removal), some should fail (after removal)
        var successfulCalls = resultArray.Where(r => r.success).ToArray();
        var failedCalls = resultArray.Where(r => !r.success).ToArray();

        // Should have both successful and failed calls
        Assert.NotEmpty(successfulCalls);
        Assert.NotEmpty(failedCalls);

        // Successful calls should return "server1"
        Assert.All(successfulCalls, result => Assert.Equal("server1", result.server));

        // Failed calls should return null
        Assert.All(failedCalls, result => Assert.Null(result.server));
    }

    [Fact]
    public async Task HashRing_ConcurrentExtensionMethodCalls_MaintainConsistency()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        var stringResults = new ConcurrentBag<string>();
        var guidResults = new ConcurrentBag<string>();
        var intResults = new ConcurrentBag<string>();

        const string stringKey = "user123";
        var guidKey = Guid.Parse("12345678-1234-1234-1234-123456789012");
        const int intKey = 12345;

        // Act - Concurrent extension method calls
        var tasks = new List<Task>();

        // String key calls
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(1, 50));
                var server = ring.GetServer(stringKey);
                stringResults.Add(server);
            }, TestContext.Current.CancellationToken));
        }

        // Guid key calls
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(1, 50));
                var server = ring.GetServer(guidKey);
                guidResults.Add(server);
            }, TestContext.Current.CancellationToken));
        }

        // Int key calls
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(1, 50));
                var server = ring.GetServer(intKey);
                intResults.Add(server);
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        // Assert - Each key type should consistently map to the same server
        var distinctStringResults = stringResults.Distinct().ToArray();
        var distinctGuidResults = guidResults.Distinct().ToArray();
        var distinctIntResults = intResults.Distinct().ToArray();

        Assert.Single(distinctStringResults);
        Assert.Single(distinctGuidResults);
        Assert.Single(distinctIntResults);

        // All results should be valid servers
        Assert.Contains(distinctStringResults[0], ring.Servers);
        Assert.Contains(distinctGuidResults[0], ring.Servers);
        Assert.Contains(distinctIntResults[0], ring.Servers);
    }
}
