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
        using var cts = new CancellationTokenSource();

        var writer = Task.Run(() =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                var server = $"server-{i % 3}";
                ring.Add(server);
                ring.CreateConfigurationSnapshot();
                _ = ring.Remove(server);
                ring.CreateConfigurationSnapshot();
                i++;
            }
        }, TestContext.Current.CancellationToken);

        // The ring always contains "anchor", so every snapshot can serve lookups.
        // Unsynchronized reads of the snapshot history would surface here as
        // InvalidOperationException ("collection was modified") or index errors.
        for (var i = 0; i < 100_000; i++)
        {
            _ = ring.GetServer(key);
            _ = ring.GetServers(key, 2).ToList();
        }

        cts.Cancel();
        await writer;
    }
}
