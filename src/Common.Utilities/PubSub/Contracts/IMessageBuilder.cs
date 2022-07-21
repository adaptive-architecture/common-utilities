namespace AdaptArch.Common.Utilities.PubSub.Contracts;

/// <summary>
/// A message builder.
/// </summary>
public interface IMessageBuilder<T>
    where T : class
{
    /// <summary>
    /// Build a message containing the data.
    /// <param name="topic">The message topic.</param>
    /// <param name="data">The message data.</param>
    /// </summary>
    IMessage<T> Build(string topic, T data);
}
