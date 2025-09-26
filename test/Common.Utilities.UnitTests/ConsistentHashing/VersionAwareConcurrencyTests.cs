namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public sealed class VersionAwareConcurrencyTests
{
    [Fact]
    public async Task ConcurrentSnapshotCreation_WithMultipleThreads_MaintainsIntegrity()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
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
    public async Task ConcurrentServerCandidateQueries_WithHistory_ReturnsConsistentResults()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 3
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");

        var testKey = Encoding.UTF8.GetBytes("concurrent-test-key");
        var results = new ConcurrentBag<ServerCandidateResult<string>>();

        var queryTasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var candidates = hashRing.GetServerCandidates(testKey);
            results.Add(candidates);
        })).ToArray();

        await Task.WhenAll(queryTasks);

        Assert.Equal(20, results.Count);

        var firstResult = results.First();
        Assert.All(results, result =>
        {
            Assert.Equal(firstResult.ConfigurationCount, result.ConfigurationCount);
            Assert.Equal(firstResult.HasHistory, result.HasHistory);
            Assert.Equal(firstResult.Servers.Count, result.Servers.Count);
            Assert.True(firstResult.Servers.SequenceEqual(result.Servers));
        });
    }

    [Fact]
    public async Task ConcurrentHistoryClear_WithQueries_MaintainsThreadSafety()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 5
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("clear-test-key");
        var queryResults = new ConcurrentBag<ServerCandidateResult<string>>();
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
                    var candidates = hashRing.GetServerCandidates(testKey);
                    queryResults.Add(candidates);
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
        Assert.False(queryResults.IsEmpty);
        Assert.Contains(queryResults, r => r.HasHistory);
        Assert.Contains(queryResults, r => !r.HasHistory);
    }

    [Fact]
    public async Task ConcurrentTryGetServerCandidates_WithVaryingHistory_ThreadSafe()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 3
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("concurrent-server-1");
        var testKey = Encoding.UTF8.GetBytes("try-get-concurrent");

        var successResults = new ConcurrentBag<ServerCandidateResult<string>>();
        var failureCount = 0;

        var snapshotTask = Task.Run(() =>
        {
            Task.Delay(25).Wait();
            hashRing.CreateConfigurationSnapshot();
            hashRing.Add("concurrent-server-2");
        }, TestContext.Current.CancellationToken);

        var queryTasks = Enumerable.Range(0, 15).Select(_ => Task.Run(() =>
        {
            if (hashRing.TryGetServerCandidates(testKey, out var result))
            {
                successResults.Add(result);
            }
            else
            {
                Interlocked.Increment(ref failureCount);
            }
        })).ToArray();

        await Task.WhenAll([snapshotTask, .. queryTasks]);

        Assert.False(successResults.IsEmpty);
        Assert.Equal(0, failureCount);
        Assert.All(successResults, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task ConcurrentMaxCandidatesQueries_WithHistory_ProducesConsistentResults()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
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
        var results = new ConcurrentBag<ServerCandidateResult<string>>();

        var queryTasks = Enumerable.Range(0, 12).Select(i => Task.Run(() =>
        {
            var maxCandidates = (i % 3) + 1;
            var candidates = hashRing.GetServerCandidates(testKey, maxCandidates);
            results.Add(candidates);
        })).ToArray();

        await Task.WhenAll(queryTasks);

        Assert.Equal(12, results.Count);
        Assert.All(results, result =>
        {
            Assert.True(result.HasHistory);
            Assert.True(result.Servers.Count <= 3);
        });
    }

    [Fact]
    public async Task ConcurrentOperations_MixedScenario_MaintainsConsistency()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 4
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("mixed-server-1");
        hashRing.Add("mixed-server-2");

        var testKey = Encoding.UTF8.GetBytes("mixed-operations-test");
        var queryResults = new ConcurrentBag<ServerCandidateResult<string>>();
        var exceptions = new ConcurrentBag<Exception>();

        var snapshotTask = Task.Run(() =>
        {
            try
            {
                Task.Delay(20).Wait();
                hashRing.CreateConfigurationSnapshot();
                Task.Delay(30).Wait();
                hashRing.Add("mixed-server-3");
                Task.Delay(20).Wait();
                hashRing.CreateConfigurationSnapshot();
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
                    var candidates = hashRing.GetServerCandidates(testKey);
                    queryResults.Add(candidates);
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
        Assert.False(queryResults.IsEmpty);

        var withHistory = queryResults.Where(r => r.HasHistory).ToList();
        var withoutHistory = queryResults.Where(r => !r.HasHistory).ToList();

        Assert.True(withHistory.Count > 0);
        Assert.True(withoutHistory.Count > 0);
    }
}
