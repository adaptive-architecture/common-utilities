using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// A registry to keep track of the handlers.
/// </summary>
public sealed class HandlerRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HandlerRegistration>> _handlers = new();
    private readonly DashedUuidProvider _uuidProvider = new();

    /// <summary>
    /// Add a new topic delegate to the registry.
    /// </summary>
    /// <typeparam name="T">The type of data handled by the handler.</typeparam>
    /// <param name="topic">The topic.</param>
    /// <param name="handler">The handler.</param>
    /// <returns>The id of the registration.</returns>
    public string Add<T>(string topic, Delegate handler)
        where T : class
    {
        var registration = new HandlerRegistration(_uuidProvider.New(), topic, handler, typeof(T));

        _ = _handlers.AddOrUpdate(topic,
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

    /// <summary>
    /// Remove a registration.
    /// </summary>
    /// <param name="registrationId">The registration to remove.</param>
    public void Remove(string registrationId)
    {
        foreach (var topicHandlers in _handlers.Values)
        {
            _ = topicHandlers.TryRemove(registrationId, out _);
        }
    }

    /// <summary>
    /// Get the handlers for a topic.
    /// </summary>
    /// <typeparam name="T">The type of data handled by the handler.</typeparam>
    /// <param name="topic">The topic.</param>
    public IEnumerable<Delegate> GetTopicHandlers<T>(string topic)
        where T : class
    {
        return _handlers.TryGetValue(topic, out var handlerRegistrations)
            ? GetMatchingHandlers<T>(handlerRegistrations.Values)
            : Array.Empty<Delegate>();
    }

    /// <summary>
    /// Get a specific registration.
    /// </summary>
    /// <param name="registrationId">The registration id.</param>
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
