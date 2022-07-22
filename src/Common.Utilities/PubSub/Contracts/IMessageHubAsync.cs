namespace AdaptArch.Common.Utilities.PubSub.Contracts;

/// <summary>
/// Contract for a Pub/Sub hub.
/// </summary>
public interface IMessageHubAsync
{
    /// <summary>
    /// Publish a message async.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="data">The data to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TMessageData"></typeparam>
    Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class;

    /// <summary>
    /// Subscribe a message handler to a topic async.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="handler">The handler for the data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TMessageData">The type of the data.</typeparam>
    /// <returns>An id that can be used to unsubscribe.</returns>
    Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken)
        where TMessageData : class;

    /// <summary>
    /// Unsubscribe a handler from a topic async.
    /// </summary>
    /// <param name="id">The subscription id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsubscribeAsync(string id, CancellationToken cancellationToken);
}
