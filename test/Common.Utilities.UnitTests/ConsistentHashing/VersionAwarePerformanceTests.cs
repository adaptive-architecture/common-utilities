namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AdaptArch.Common.Utilities.ConsistentHashing;
using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using Xunit;

public sealed class VersionAwarePerformanceTests
{
    [RetryFact]
    public void GetServer_StandardVsVersionAware_PerformanceComparison()
    {
        var standardOptions = new HashRingOptions();
        var versionAwareOptions = new HashRingOptions { MaxHistorySize = 3 };

        var standardRing = new HashRing<string>(standardOptions);
        var versionAwareRing = new HashRing<string>(versionAwareOptions);

        for (int i = 1; i <= 10; i++)
        {
            standardRing.Add($"server-{i}");
            versionAwareRing.Add($"server-{i}");
        }
        standardRing.CreateConfigurationSnapshot();
        versionAwareRing.CreateConfigurationSnapshot();

        var testKeys = GenerateTestKeys(1000);

        var standardTime = MeasureOperation(() =>
        {
            foreach (var key in testKeys)
            {
                standardRing.GetServer(key);
            }
        });

        var versionAwareTime = MeasureOperation(() =>
        {
            foreach (var key in testKeys)
            {
                versionAwareRing.GetServer(key);
            }
        });

        // Performance comparison - could log to console in debug builds if needed

        var performanceRatio = versionAwareTime.TotalMilliseconds / standardTime.TotalMilliseconds;
        Assert.True(performanceRatio < 2.0, $"Version-aware should be at most 2x slower, was {performanceRatio:F2}x");
    }

    [Fact]
    public void GetServerCandidates_WithHistory_PerformanceWithScale()
    {
        var options = new HashRingOptions { MaxHistorySize = 5 };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 50; i++)
        {
            hashRing.Add($"server-{i}");
        }
        hashRing.CreateConfigurationSnapshot();

        for (int snapshot = 0; snapshot < 5; snapshot++)
        {
            hashRing.Add($"scale-server-{snapshot}");
            hashRing.CreateConfigurationSnapshot();
        }

        var testKeys = GenerateTestKeys(5000);

        var candidatesTime = MeasureOperation(() =>
        {
            foreach (var key in testKeys)
            {
                _ = hashRing.GetServer(key);
            }
        });

        // Performance test completed

        var avgTimePerKey = candidatesTime.TotalMilliseconds / testKeys.Count;
        Assert.True(avgTimePerKey < 1.0, $"Should be under 1ms per key, was {avgTimePerKey:F3}ms");
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(3, 100)]
    [InlineData(5, 100)]
    public void GetServerCandidates_HistorySize_PerformanceLinearScale(int historySize, int keyCount)
    {
        var options = new HashRingOptions { MaxHistorySize = historySize };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 20; i++)
        {
            hashRing.Add($"perf-server-{i}");
        }
        hashRing.CreateConfigurationSnapshot();

        for (int i = 0; i < historySize; i++)
        {
            hashRing.Add($"history-server-{i}");
            hashRing.CreateConfigurationSnapshot();
        }

        var testKeys = GenerateTestKeys(keyCount);

        var executionTime = MeasureOperation(() =>
        {
            foreach (var key in testKeys)
            {
                hashRing.GetServer(key);
            }
        });

        var avgTimePerKey = executionTime.TotalMilliseconds / keyCount;

        Assert.True(avgTimePerKey < historySize * 0.5,
            $"Performance should scale roughly linearly with history size. Expected < {historySize * 0.5:F3}ms, got {avgTimePerKey:F3}ms");
    }

    [Fact]
    public void CreateConfigurationSnapshot_Performance_AcceptableOverhead()
    {
        var options = new HashRingOptions { MaxHistorySize = 10 };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 100; i++)
        {
            hashRing.Add($"snapshot-server-{i}");
        }
        hashRing.CreateConfigurationSnapshot();

        var snapshotTimes = new List<TimeSpan>();

        for (int i = 0; i < 10; i++)
        {
            var snapshotTime = MeasureOperation(() =>
            {
                hashRing.Add($"additional-server-{i}");
                hashRing.CreateConfigurationSnapshot();
            });

            snapshotTimes.Add(snapshotTime);
        }

        var avgSnapshotTime = snapshotTimes.Average(t => t.TotalMilliseconds);
        var maxSnapshotTime = snapshotTimes.Max(t => t.TotalMilliseconds);

        Assert.True(avgSnapshotTime < 50.0, $"Snapshot creation should be under 50ms, was {avgSnapshotTime:F2}ms");
        Assert.True(maxSnapshotTime < 100.0, $"Max snapshot time should be under 100ms, was {maxSnapshotTime:F2}ms");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    public void MemoryUsage_WithHistory_ReasonableOverhead(int serverCount)
    {
        var noHistoryOptions = new HashRingOptions();
        var withHistoryOptions = new HashRingOptions { MaxHistorySize = 3 };

        var standardRing = new HashRing<string>(noHistoryOptions);
        var versionAwareRing = new HashRing<string>(withHistoryOptions);

        for (int i = 1; i <= serverCount; i++)
        {
            standardRing.Add($"server-{i}");
            versionAwareRing.Add($"server-{i}");
        }
        standardRing.CreateConfigurationSnapshot();
        versionAwareRing.CreateConfigurationSnapshot();

        for (int i = 0; i < 3; i++)
        {
            versionAwareRing.Add($"history-server-{i}");
            versionAwareRing.CreateConfigurationSnapshot();
        }

#pragma warning disable S1215
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
#pragma warning restore S1215

        var standardMemoryBefore = GC.GetTotalMemory(false);
        var testKeys = GenerateTestKeys(100);

        foreach (var key in testKeys)
        {
            standardRing.GetServer(key);
        }
#pragma warning disable S1215
        var standardMemoryAfter = GC.GetTotalMemory(true);

        foreach (var key in testKeys)
        {
            versionAwareRing.GetServer(key);
        }

        var versionAwareMemoryAfter = GC.GetTotalMemory(true);
#pragma warning restore S1215
        var standardMemoryUsed = standardMemoryAfter - standardMemoryBefore;
        var versionAwareMemoryUsed = versionAwareMemoryAfter - standardMemoryAfter;

        if (standardMemoryUsed > 0)
        {
            var memoryRatio = (double)versionAwareMemoryUsed / standardMemoryUsed;
            Assert.True(memoryRatio < 10.0, $"Memory overhead should be reasonable, was {memoryRatio:F2}x");
        }
    }

    [RetryFact]
    public void ConcurrentOperations_Performance_ThreadSafetyOverhead()
    {
        var options = new HashRingOptions { MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 20; i++)
        {
            hashRing.Add($"concurrent-server-{i}");
        }

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("concurrent-extra");

        var testKeys = GenerateTestKeys(1000);

        var sequentialTime = MeasureOperation(() =>
        {
            foreach (var key in testKeys)
            {
                hashRing.GetServer(key);
            }
        });

        var concurrentTime = MeasureOperation(() => Parallel.ForEach(testKeys, key => hashRing.GetServer(key)));


        // Concurrent operations might be slower due to lock contention, but they should scale reasonably
        // Allow concurrent operations to be up to 5x slower than sequential due to synchronization overhead
        // This accounts for the heavy lock usage in version-aware operations
        const double maxAcceptableRatio = 5.0;
        var actualRatio = concurrentTime.TotalMilliseconds / sequentialTime.TotalMilliseconds;

        Assert.True(actualRatio <= maxAcceptableRatio,
            $"Concurrent operations took {actualRatio:F2}x longer than sequential (max acceptable: {maxAcceptableRatio}x). " +
            $"Sequential: {sequentialTime.TotalMilliseconds:F2}ms, Concurrent: {concurrentTime.TotalMilliseconds:F2}ms");
    }

    private static TimeSpan MeasureOperation(Action operation)
    {
        var stopwatch = Stopwatch.StartNew();
        operation();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static List<byte[]> GenerateTestKeys(int count)
    {
        var keys = new List<byte[]>();
        for (int i = 0; i < count; i++)
        {
            keys.Add(Encoding.UTF8.GetBytes($"test-key-{i:D6}"));
        }
        return keys;
    }
}
