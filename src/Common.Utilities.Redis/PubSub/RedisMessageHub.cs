using AdaptArch.Common.Utilities.Extensions;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.Redis.Utilities;
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
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);
        _connectionMultiplexer = connectionMultiplexer;
    }

    /// <inheritdoc />
    public override void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class
    {
        var message = SerializeMessage(topic, data);
        _ = GetSubscriber().Publish(topic.ToChannel(), message);
    }

    /// <inheritdoc />
    public override string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        var redisHandler = WrapAsRedisHandler(handler);
        var id = _registry.Add<TMessageData>(topic, redisHandler);
        GetSubscriber().Subscribe(topic.ToChannel(), redisHandler);
        return id;
    }

    /// <inheritdoc />
    public override void Unsubscribe(string id)
    {
        var registration = _registry.GetRegistration(id);
        if (registration is { Handler: Action<RedisChannel, RedisValue> redisHandler })
        {
            GetSubscriber().Unsubscribe(registration.Topic.ToChannel(), redisHandler);
            _registry.Remove(id);
        }
    }

    /// <inheritdoc />
    public override async Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class
    {
        var message = SerializeMessage(topic, data);
        _ = await GetSubscriber().PublishAsync(topic.ToChannel(), message).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
    }

    /// <inheritdoc />
    public override async Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken)
        where TMessageData : class
    {
        var redisHandler = WrapAsRedisHandler(handler);
        var id = _registry.Add<TMessageData>(topic, redisHandler);
        await GetSubscriber().SubscribeAsync(topic.ToChannel(), redisHandler).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
        return id;
    }

    /// <inheritdoc />
    public override async Task UnsubscribeAsync(string id, CancellationToken cancellationToken)
    {
        var registration = _registry.GetRegistration(id);
        if (registration is { Handler: Action<RedisChannel, RedisValue> redisHandler })
        {
            await GetSubscriber().UnsubscribeAsync(registration.Topic.ToChannel(), redisHandler).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            _registry.Remove(id);
        }
    }

    private RedisValue SerializeMessage<TMessageData>(string topic, TMessageData data) where TMessageData : class
    {
        var message = Options.GetMessageBuilder<TMessageData>().Build(topic, data);
        return Options.DataSerializer.Serialize(message);
    }

    private ISubscriber GetSubscriber() => _connectionMultiplexer.GetSubscriber();

    private Message<TMessageData> DeserializeMessage<TMessageData>(RedisValue data) where TMessageData : class
    {
        return Options.DataSerializer.Deserialize<Message<TMessageData>>(data)!;
    }

    private Action<RedisChannel, RedisValue> WrapAsRedisHandler<TMessageData>(MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        var safeHandler = WrapHandler(handler);
        return (channel, value) =>
        {
            Message<TMessageData> message;
            try
            {
                // Deserialization runs inside the Redis subscription callback; a malformed
                // payload must route to the error handler instead of throwing here.
                message = DeserializeMessage<TMessageData>(value);
            }
            catch (Exception ex)
            {
                InvokeErrorHandler(channel, ex);
                return;
            }

            safeHandler.Invoke(message, CancellationToken.None).Forget();
        };
    }

    private void InvokeErrorHandler(RedisChannel channel, Exception ex)
    {
        try
        {
            var errorMessage = new Message<object>(String.Empty, DateTime.UtcNow, channel.ToString(), ex);
            Options.OnMessageHandlerError?.Invoke(ex, errorMessage);
        }
        catch
        {
            // If the error callback itself fails there is nothing more to do.
        }
    }
}
