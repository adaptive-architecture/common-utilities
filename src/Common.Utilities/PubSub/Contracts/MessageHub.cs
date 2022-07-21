using System.Collections.Concurrent;

namespace AdaptArch.Common.Utilities.PubSub.Contracts
{
    internal class MessageHub
    {
        private class HandlerRegistration
        {
            public string Id { get; set; }
            public Delegate MessageHandler { get; set; }

            public Type MessageType { get; set; }

            public HandlerRegistration(string id, Delegate messageHandler, Type messageType)
            {
                Id = id;
                MessageHandler = messageHandler;
                MessageType = messageType;
            }
        }

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HandlerRegistration>> _handlers = new();

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

        public void Subscribe(string id)
        {
            foreach (var topicHandlers in _handlers.Values)
            {
                topicHandlers.TryRemove(id, out _);
            }
        }

        public async Task Publish<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
            where TMessageData : class
        {
            if (_handlers.TryGetValue(topic, out var handlerRegistrations))
            {
                var handlers = GetMatchingHandlers<TMessageData>(handlerRegistrations.Values);
                await InvokeHandlers(handlers, new Message<TMessageData>(topic, data), cancellationToken).ConfigureAwait(false);
            }
        }

        public Action<Exception, IMessage<object>>? OnMessageHandlerError { get; set; }

        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;


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
                    MaxDegreeOfParallelism = Math.Max(1, MaxDegreeOfParallelism),
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
                    OnMessageHandlerError?.Invoke(ex, message);
                }
                catch
                {
                    // If the delegate call fails we have nothing to do.
                }
            }
        }
    }
}
