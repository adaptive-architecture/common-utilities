namespace AdaptArch.Common.Utilities.PubSub.Contracts;

/// <summary>
/// Contract for a Pub/Sub hub.
/// </summary>
public interface IMessageHub
{
    /// <summary>
    /// Publish a message.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="data">The data to publish.</param>
    /// <typeparam name="TMessageData"></typeparam>
    void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class;

    /// <summary>
    /// Subscribe a message handler to a topic.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="handler">The handler for the data.</param>
    /// <typeparam name="TMessageData">The type of the data.</typeparam>
    /// <returns>An id that can be used to unsubscribe.</returns>
    string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class;

    /// <summary>
    /// Unsubscribe a handler from a topic.
    /// </summary>
    /// <param name="id">The subscription id.</param>
    void Unsubscribe(string id);
}
