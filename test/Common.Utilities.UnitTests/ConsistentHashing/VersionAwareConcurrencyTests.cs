namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class VersionAwareConcurrencyTests
{
    [Fact]
    public async Task ConcurrentSnapshotCreation_WithMultipleThreads_MaintainsIntegrity()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 10
        };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 5; i++)
        {
            hashRing.Add($"server-{i}");
        }

        var exceptions = new ConcurrentBag<Exception>();
        var successCount = 0;

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < 5; j++)
                {
                    hashRing.CreateConfigurationSnapshot();
                    hashRing.Add($"thread-{i}-server-{j}");
                    Interlocked.Increment(ref successCount);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.True(hashRing.HistoryCount <= options.MaxHistorySize);
        Assert.True(exceptions.All(ex => ex is HashRingHistoryLimitExceededException));
    }

    [Fact]
    public async Task ConcurrentServerQueries_WithHistory_ReturnsConsistentResults()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 3
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("concurrent-test-key");
        var results = new ConcurrentBag<string>();

        var queryTasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var server = hashRing.GetServer(testKey);
            results.Add(server);
        })).ToArray();

        await Task.WhenAll(queryTasks);

        Assert.Equal(20, results.Count);

        var firstResult = results.First();
        // All concurrent queries should return same server for same key
        Assert.All(results, result => Assert.Equal(firstResult, result));
    }

    [Fact]
    public async Task ConcurrentHistoryClear_WithQueries_MaintainsThreadSafety()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 5
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("clear-test-key");
        var successfulQueries = 0;
        var failedQueries = 0;
        var exceptions = new ConcurrentBag<Exception>();

        var clearTask = Task.Run(() =>
        {
            try
            {
                Task.Delay(50).Wait();
                hashRing.ClearHistory();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        var queryTasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    if (hashRing.TryGetServer(testKey, out var server))
                    {
                        Interlocked.Increment(ref successfulQueries);
                        Assert.NotNull(server);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedQueries);
                    }
                    Task.Delay(10).Wait();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll([clearTask, .. queryTasks]);

        Assert.Empty(exceptions);
        // Some queries should succeed (before clear) and some should fail (after clear)
        Assert.True(successfulQueries > 0);
        Assert.True(failedQueries > 0);
    }

    [Fact]
    public async Task ConcurrentTryGetServer_WithVaryingHistory_ThreadSafe()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 3
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("concurrent-server-1");
        hashRing.CreateConfigurationSnapshot();
        var testKey = Encoding.UTF8.GetBytes("try-get-concurrent");

        var successResults = new ConcurrentBag<string>();
        var failureCount = 0;

        var snapshotTask = Task.Run(() =>
        {
            Task.Delay(25).Wait();
            hashRing.Add("concurrent-server-2");
            hashRing.CreateConfigurationSnapshot();
        }, TestContext.Current.CancellationToken);

        var queryTasks = Enumerable.Range(0, 15).Select(_ => Task.Run(() =>
        {
            if (hashRing.TryGetServer(testKey, out var server))
            {
                successResults.Add(server);
            }
            else
            {
                Interlocked.Increment(ref failureCount);
            }
        })).ToArray();

        await Task.WhenAll([snapshotTask, .. queryTasks]);

        Assert.False(successResults.IsEmpty);
        // Most queries should succeed after snapshot is created
        Assert.True(failureCount < 15);
        Assert.All(successResults, server => Assert.NotNull(server));
    }

    [Fact]
    public async Task ConcurrentGetServers_WithHistory_ProducesConsistentResults()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 2
        };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 4; i++)
        {
            hashRing.Add($"max-server-{i}");
        }

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("max-server-5");

        var testKey = Encoding.UTF8.GetBytes("max-candidates-test");
        var results = new ConcurrentBag<List<string>>();

        var queryTasks = Enumerable.Range(0, 12).Select(i => Task.Run(() =>
        {
            var count = (i % 3) + 1;
            var servers = hashRing.GetServers(testKey, count).ToList();
            results.Add(servers);
        })).ToArray();

        await Task.WhenAll(queryTasks);

        Assert.Equal(12, results.Count);
        Assert.All(results, serverList =>
        {
            Assert.NotEmpty(serverList);
            Assert.True(serverList.Count <= 3);
        });
    }

    [Fact]
    public async Task ConcurrentOperations_MixedScenario_MaintainsConsistency()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 4
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("mixed-server-1");
        hashRing.Add("mixed-server-2");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("mixed-operations-test");
        var successfulQueries = 0;
        var failedQueries = 0;
        var exceptions = new ConcurrentBag<Exception>();

        var snapshotTask = Task.Run(() =>
        {
            try
            {
                Task.Delay(20).Wait();
                Task.Delay(30).Wait();
                hashRing.Add("mixed-server-3");
                hashRing.CreateConfigurationSnapshot();
                Task.Delay(20).Wait();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        var clearTask = Task.Run(() =>
        {
            try
            {
                Task.Delay(100).Wait();
                hashRing.ClearHistory();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        var queryTasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    if (hashRing.TryGetServer(testKey, out var server))
                    {
                        Interlocked.Increment(ref successfulQueries);
                        Assert.NotNull(server);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedQueries);
                    }
                    Task.Delay(15).Wait();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll([snapshotTask, clearTask, .. queryTasks]);

        Assert.Empty(exceptions);
        // Some queries should succeed (with snapshots) and some fail (after clear)
        Assert.True(successfulQueries > 0);
        Assert.True(failedQueries > 0);
    }
}
