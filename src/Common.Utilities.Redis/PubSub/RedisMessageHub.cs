﻿using AdaptArch.Common.Utilities.Extensions;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.PubSub.Implementations.Internals;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.PubSub;

/// <summary>
/// A Redis based implementation of <see cref="IMessageHub"/> and <see cref="IMessageHubAsync"/>.
/// </summary>
public class RedisMessageHub : MessageHub<RedisMessageHubOptions>
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly HandlerRegistry _registry = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="connectionMultiplexer">An instance of <see cref="IConnectionMultiplexer"/>.</param>
    /// <param name="options">The hub options.</param>
    public RedisMessageHub(IConnectionMultiplexer connectionMultiplexer, RedisMessageHubOptions options)
        :base(options)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
    }

    /// <inheritdoc />
    public override void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class
    {
        var message = SerializeMessage(topic, data);
        _ = GetSubscriber().Publish(topic, message);
    }

    /// <inheritdoc />
    public override string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        var redisHandler = WrapAsRedisHandler(handler);
        var id = _registry.Add<TMessageData>(topic, redisHandler);
        GetSubscriber().Subscribe(topic, redisHandler);
        return id;
    }

    /// <inheritdoc />
    public override void Unsubscribe(string id)
    {
        var registration = _registry.GetRegistration(id);
        if (registration is { Handler: Action<RedisChannel, RedisValue> redisHandler })
        {
            GetSubscriber().Unsubscribe(registration.Topic, redisHandler);
        }
    }

    /// <inheritdoc />
    public override async Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class
    {
        var message = SerializeMessage(topic, data);
        _ = await GetSubscriber().PublishAsync(topic, message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken)
        where TMessageData : class
    {
        var redisHandler = WrapAsRedisHandler(handler);
        var id = _registry.Add<TMessageData>(topic, redisHandler);
        await GetSubscriber().SubscribeAsync(topic, redisHandler).ConfigureAwait(false);
        return id;
    }

    /// <inheritdoc />
    public override async Task UnsubscribeAsync(string id, CancellationToken cancellationToken)
    {
        var registration = _registry.GetRegistration(id);
        if (registration is { Handler: Action<RedisChannel, RedisValue> redisHandler })
        {
            await GetSubscriber().UnsubscribeAsync(registration.Topic, redisHandler).ConfigureAwait(false);
        }
    }

    private RedisValue SerializeMessage<TMessageData>(string topic, TMessageData data) where TMessageData : class
    {
        var message = Options.GetMessageBuilder<TMessageData>().Build(topic, data);
        return Options.DataSerializer.Serialize(message);
    }

    private ISubscriber GetSubscriber() => _connectionMultiplexer.GetSubscriber();

    private IMessage<TMessageData> DeserializeMessage<TMessageData>(RedisValue data) where TMessageData : class
    {
        return Options.DataSerializer.Deserialize<Message<TMessageData>>(data);
    }

    private Action<RedisChannel, RedisValue> WrapAsRedisHandler<TMessageData>(MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        var safeHandler = WrapHandler(handler);
        return (_, value) =>
        {
            var message = DeserializeMessage<TMessageData>(value);
            safeHandler.Invoke(message, CancellationToken.None).Forget();
        };
    }
}
