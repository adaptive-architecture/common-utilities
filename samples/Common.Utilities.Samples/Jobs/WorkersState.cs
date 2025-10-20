using System.Collections.Concurrent;

namespace AdaptArch.Common.Utilities.Samples.Jobs;

internal class WorkersState
{
    public ConcurrentBag<int> Numbers { get; set; } = [];
}
