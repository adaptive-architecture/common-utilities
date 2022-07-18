namespace AdaptArch.Common.Utilities.PubSub.Contracts;

/// <summary>
/// Async handler for a message.
/// </summary>
/// <param name="message">The message to be handled.</param>
/// <param name="cancellationToken">The cancellation token.</param>
public delegate Task MessageHandler<in TMessage>(IMessage<TMessage> message, CancellationToken cancellationToken) where TMessage : class;
