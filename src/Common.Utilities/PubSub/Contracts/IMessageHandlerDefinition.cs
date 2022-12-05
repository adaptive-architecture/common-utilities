namespace AdaptArch.Common.Utilities.PubSub.Contracts;

/// <summary>
/// Definition for a message handler.
/// </summary>
public interface IMessageHandlerDefinition<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// The topic of the messages to handle.
    /// </summary>
    string Topic { get; }

    /// <summary>
    /// The message handler.
    /// </summary>
    /// <param name="message">The message to be handled.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleAsync(IMessage<TMessage> message, CancellationToken cancellationToken);
}
