using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public sealed class HashRingSnapshotConcurrencySpecs
{
    [Fact]
    public async Task GetServer_Should_Be_Thread_Safe_While_Snapshots_Are_Created()
    {
        var ring = new HashRing<string>();
        ring.Add("anchor");
        ring.CreateConfigurationSnapshot();

        var key = System.Text.Encoding.UTF8.GetBytes("some-key");

        // The writer performs a bounded number of mutation cycles; the reader runs
        // concurrently until the writer finishes. Unsynchronized reads of the snapshot
        // history would surface here as InvalidOperationException ("collection was
        // modified") or index errors. The ring always contains "anchor", so every
        // snapshot can serve lookups.
        const int mutationCycles = 500;
        var writer = Task.Run(() =>
        {
            for (var i = 0; i < mutationCycles; i++)
            {
                var server = $"server-{i % 3}";
                ring.Add(server);
                ring.CreateConfigurationSnapshot();
                _ = ring.Remove(server);
                ring.CreateConfigurationSnapshot();
            }
        }, TestContext.Current.CancellationToken);

        var reads = 0;
        string lastServer = null;
        while (!writer.IsCompleted)
        {
            lastServer = ring.GetServer(key);
            Assert.NotEmpty(ring.GetServers(key, 2));
            reads++;
        }

        await writer;

        // The concurrent reads completed without throwing and always resolved a server.
        Assert.True(reads > 0);
        Assert.NotNull(lastServer);
    }
}
