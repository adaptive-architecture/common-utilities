﻿using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// A type that when implemented offers the functionality of <see cref="IMessageHub"/> and <seealso cref="IMessageHubAsync"/>.
/// </summary>
public abstract class MessageHub<TOptions> : IMessageHub, IMessageHubAsync
    where TOptions : MessageHubOptions
{
    /// <summary>
    /// The message hub options.
    /// </summary>
    protected readonly TOptions Options;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Message hub options.</param>
    protected MessageHub(TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    /// <inheritdoc />
    public abstract void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class;

    /// <inheritdoc />
    public abstract string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class;

    /// <inheritdoc />
    public abstract void Unsubscribe(string id);

    /// <inheritdoc />
    public abstract Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class;

    /// <inheritdoc />
    public abstract Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken)
        where TMessageData : class;

    /// <inheritdoc />
    public abstract Task UnsubscribeAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Wrap the original handler in a try/catch block to prevent errors from bubbling.
    /// </summary>
    /// <param name="handler">The original handler for the data.</param>
    /// <typeparam name="TMessageData">The type of the data.</typeparam>
    /// <returns>The wrapped handler for the data.</returns>
    protected MessageHandler<TMessageData> WrapHandler<TMessageData>(MessageHandler<TMessageData> handler)
        where TMessageData : class
    {
        return (message, token) => SafeInvokeHandler(handler, message, Options, token);
    }

    private static async Task SafeInvokeHandler<TMessageData>(MessageHandler<TMessageData> handler, IMessage<TMessageData> message, TOptions options, CancellationToken cancellationToken)
        where TMessageData : class
    {
        try
        {
            await handler.Invoke(message, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
        }
        catch (Exception ex)
        {
            try
            {
                options.OnMessageHandlerError?.Invoke(ex, message);
            }
            catch
            {
                // If the delegate call fails we have nothing to do.
            }
        }
    }
}
