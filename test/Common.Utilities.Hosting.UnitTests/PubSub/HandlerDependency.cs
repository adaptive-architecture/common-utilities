using System.Collections.Concurrent;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub;

public class HandlerDependency
{
    private readonly ConcurrentDictionary<string, int> _calls = new();

    public void RegisterCall(string @class, string method, string topic)
        => _calls.AddOrUpdate($"{@class}`{method}`{topic}", _ => 1, (_, v) => ++v);

    public int CountCall(string @class, string method, string topic)
        => _calls.GetOrAdd($"{@class}`{method}`{topic}", _ => 0);
}
