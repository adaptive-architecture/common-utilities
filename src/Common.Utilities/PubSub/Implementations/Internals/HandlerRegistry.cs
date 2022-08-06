using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

internal class HandlerRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HandlerRegistration>> _handlers = new();
    private readonly IUuidProvider _uuidProvider = new DashedUuidProvider();

    public string Add<T>(string topic, Delegate handler)
        where T : class
    {
        var registration = new HandlerRegistration(_uuidProvider.New(), topic, handler, typeof(T));

        _handlers.AddOrUpdate(topic,
            _ =>
            {
                var topicHandlers = new ConcurrentDictionary<string, HandlerRegistration>();
                topicHandlers.TryAdd(registration.Id, registration);
                return topicHandlers;
            },
            (_, existing) =>
            {
                existing.TryAdd(registration.Id, registration);
                return existing;
            }
        );

        return registration.Id;
    }

    public void Remove(string registrationId)
    {
        foreach (var topicHandlers in _handlers.Values)
        {
            topicHandlers.TryRemove(registrationId, out _);
        }
    }

    public IEnumerable<Delegate> GetTopicHandlers<T>(string topic)
        where T : class
    {
        return _handlers.TryGetValue(topic, out var handlerRegistrations)
            ? GetMatchingHandlers<T>(handlerRegistrations.Values)
            : Array.Empty<Delegate>();
    }

    public HandlerRegistration? GetRegistration(string registrationId)
    {
        foreach (var topicHandlers in _handlers.Values)
        {
            if (topicHandlers.TryGetValue(registrationId, out var registration))
            {
                return registration;
            }
        }

        return null;
    }

    private static IEnumerable<Delegate> GetMatchingHandlers<T>(IEnumerable<HandlerRegistration> handlerRegistrations)
        where T : class
        => handlerRegistrations
            .Where(w => w.HandledType.IsAssignableFrom(typeof(T)))
            .Select(s => s.Handler);
}
