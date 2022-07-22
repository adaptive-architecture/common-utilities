using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <inheritdoc />
public class MessageBuilder<T>: IMessageBuilder<T>
    where T : class
{
    /// <inheritdoc />
    private class Message<TData> : IMessage<TData> where TData : class
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

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUuidProvider _uuidProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dateTimeProvider">A date time provider.</param>
    /// <param name="uuidProvider">An UUID provider.</param>
    public MessageBuilder(IDateTimeProvider dateTimeProvider, IUuidProvider uuidProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _uuidProvider = uuidProvider;
    }

    /// <inheritdoc />
    public IMessage<T> Build(string topic, T data)
    {
        return new Message<T>(
            _uuidProvider.New(),
            _dateTimeProvider.UtcNow,
            topic,
            data
        );
    }

}
