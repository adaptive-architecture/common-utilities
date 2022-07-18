namespace AdaptArch.Common.Utilities.PubSub.Contracts
{
    /// <inheritdoc/>
    public class Message<T> : IMessage<T> where T : class
    {
        /// <summary>
        /// Instance constructor.
        /// </summary>
        /// <param name="topic">The message topic.</param>
        /// <param name="data">The message data.</param>
        public Message(string topic, T data)
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            Topic = topic;
            Data = data;
        }


        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public string Topic { get; }

        /// <inheritdoc/>
        public DateTime Timestamp { get; }

        /// <inheritdoc/>
        public T Data { get; }
}
}
