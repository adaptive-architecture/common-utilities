using AdaptArch.Common.Utilities.Extensions;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// An in-process implementation of <see cref="IMessageHub"/> and <seealso cref="IMessageHubAsync"/>.
/// This is intended mostly for unit testing of rapid prototyping.
/// </summary>
public class InProcessMessageHub : MessageHub<InProcessMessageHubOptions>
{
    private readonly HandlerRegistry _registry = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public InProcessMessageHub(InProcessMessageHubOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class
    {
        PublishAsync(topic, data, CancellationToken.None).Forget();
    }

    /// <inheritdoc />
    public override string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        var safeHandler = WrapHandler(handler);
        return _registry.Add<TMessageData>(topic, safeHandler);
    }

    /// <inheritdoc />
    public override void Unsubscribe(string id)
        => _registry.Remove(id);

    /// <inheritdoc />
    public override async Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class
    {
        var handlers = _registry.GetTopicHandlers<TMessageData>(topic).OfType<MessageHandler<TMessageData>>();
        var messageBuilder = Options.GetMessageBuilder<TMessageData>();

        await Parallel.ForEachAsync(
            handlers,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Options.MaxDegreeOfParallelism),
                CancellationToken = cancellationToken
            },
            async (h, ct) => await h.Invoke(messageBuilder.Build(topic, data), ct).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding)
        ).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
    }

    /// <inheritdoc />
    public override Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken)
        where TMessageData : class
    {
        return Task.FromResult(Subscribe(topic, handler));
    }

    /// <inheritdoc />
    public override Task UnsubscribeAsync(string id, CancellationToken cancellationToken)
    {
        Unsubscribe(id);
        return Task.CompletedTask;
    }
}
