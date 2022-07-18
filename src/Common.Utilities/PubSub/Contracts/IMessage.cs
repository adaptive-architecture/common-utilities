namespace AdaptArch.Common.Utilities.PubSub.Contracts;

/// <summary>
/// A pub/sub message.
/// </summary>
public interface IMessage<out T>
    where T : class
{
    /// <summary>
    /// Unique id.
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// Message topic.
    /// </summary>
    public string Topic { get; }
    /// <summary>
    /// The moment the message was created.
    /// </summary>
    public DateTime Timestamp { get; }
    /// <summary>
    /// Any business related message data.
    /// </summary>
    public T Data { get; }
}
