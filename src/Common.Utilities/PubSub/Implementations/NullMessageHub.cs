using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <inheritdoc />
public sealed class NullMessageHub : MessageHub<NullMessageHubOptions>
{
    /// <inheritdoc />
    private const string NullSubscriptionId = "null-subscription";

    /// <summary>
    /// Initializes a new instance of the <see cref="NullMessageHub"/> class.
    /// </summary>
    /// <param name="options">The configuration options for the null message hub.</param>
    public NullMessageHub(NullMessageHubOptions options) : base(options)
    {
    }

    /// <inheritdoc/>
    public override void Publish<TMessageData>(string topic, TMessageData data) where TMessageData : class
    {
        // No-op: swallow the publish command
    }

    /// <inheritdoc/>
    public override string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler) where TMessageData : class
    {
        // No-op: swallow the subscribe command and return a dummy subscription ID
        return NullSubscriptionId;
    }

    /// <inheritdoc/>
    public override void Unsubscribe(string id)
    {
        // No-op: swallow the unsubscribe command
    }

    /// <inheritdoc/>
    public override Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken) where TMessageData : class
    {
        // No-op: swallow the publish command and return completed task
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken) where TMessageData : class
    {
        // No-op: swallow the subscribe command and return dummy subscription ID in completed task
        return Task.FromResult(NullSubscriptionId);
    }

    /// <inheritdoc/>
    public override Task UnsubscribeAsync(string id, CancellationToken cancellationToken)
    {
        // No-op: swallow the unsubscribe command and return completed task
        return Task.CompletedTask;
    }
}
