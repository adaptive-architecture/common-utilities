using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

/// <inheritdoc />
internal class Message<TData> : IMessage<TData> where TData : class
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id">Message id.</param>
    /// <param name="timestamp">Message timestamp.</param>
    /// <param name="topic">Message topic.</param>
    /// <param name="data">Message data.</param>
    public Message(string id, DateTime timestamp, string topic, TData data)
    {
        Id = id;
        Timestamp = timestamp;
        Topic = topic;
        Data = data;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Topic { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public TData Data { get; }
}
