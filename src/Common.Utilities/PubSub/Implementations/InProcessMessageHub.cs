using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.Extensions;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// An in-process implementation of <see cref="IMessageHub"/> and <seealso cref="IMessageHubAsync"/>.
/// This is intended mostly for unit testing of rapid prototyping.
/// </summary>
public class InProcessMessageHub : IMessageHub, IMessageHubAsync
{
    private class HandlerRegistration
    {
        public string Id { get; }
        public Delegate MessageHandler { get; }

        public Type MessageType { get; }

        public HandlerRegistration(string id, Delegate messageHandler, Type messageType)
        {
            Id = id;
            MessageHandler = messageHandler;
            MessageType = messageType;
        }
    }

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HandlerRegistration>> _handlers = new();

    private readonly InProcessMessageHubOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public InProcessMessageHub(InProcessMessageHubOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }


    /// <inheritdoc />
    public void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class
    {
        PublishAsync(topic, data, CancellationToken.None).Forget();
    }

    /// <inheritdoc />
    public string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        var registration = new HandlerRegistration(Guid.NewGuid().ToString("N"), handler, typeof(TMessageData));
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

    /// <inheritdoc />
    public void Unsubscribe(string id)
    {
        foreach (var topicHandlers in _handlers.Values)
        {
            topicHandlers.TryRemove(id, out _);
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class
    {
        if (_handlers.TryGetValue(topic, out var handlerRegistrations))
        {
            var message = _options.GetMessageBuilder<TMessageData>().Build(topic, data);
            var handlers = GetMatchingHandlers<TMessageData>(handlerRegistrations.Values);
            await InvokeHandlers(handlers, message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler,
        CancellationToken cancellationToken)
        where TMessageData : class
    {
        var subscriptionId = Subscribe(topic, handler);
        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc />
    public Task UnsubscribeAsync(string id, CancellationToken cancellationToken)
    {
        Unsubscribe(id);
        return Task.CompletedTask;
    }

    private static IEnumerable<MessageHandler<T>> GetMatchingHandlers<T>(IEnumerable<HandlerRegistration> handlerRegistrations)
        where T : class
        => handlerRegistrations
            .Where(w => w.MessageType.IsAssignableFrom(typeof(T)))
            .Select(s => s.MessageHandler)
            .Cast<MessageHandler<T>>();

    private async Task InvokeHandlers<T>(IEnumerable<MessageHandler<T>> handlers, IMessage<T> message, CancellationToken cancellationToken)
        where T : class
    {
        await Parallel.ForEachAsync(
            handlers,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, _options.MaxDegreeOfParallelism),
                CancellationToken = cancellationToken
            },
            (h, ct) => InvokeHandler(h, message, ct)
        ).ConfigureAwait(false);
    }

    private async ValueTask InvokeHandler<T>(MessageHandler<T> handler, IMessage<T> message, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            await handler.Invoke(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                _options.OnMessageHandlerError?.Invoke(ex, message);
            }
            catch
            {
                // If the delegate call fails we have nothing to do.
            }
        }
    }
}
