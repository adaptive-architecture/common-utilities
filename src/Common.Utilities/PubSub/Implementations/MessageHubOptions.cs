using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// Base configuration options for <see cref="IMessageHub"/> and <seealso cref="IMessageHubAsync"/>.
/// </summary>
public abstract class MessageHubOptions
{
    /// <summary>
    /// Action to handle any exception thrown in the hub.
    /// </summary>
    public Action<Exception, IMessage<object>>? OnMessageHandlerError { get; set; }

    /// <summary>
    /// Accessor method for the message builder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public virtual IMessageBuilder<T> GetMessageBuilder<T>()
        where T : class
    {
        return DefaultMessageBuilder<T>.Instance;
    }

    // The default builder and its providers are stateless; cache one per message type
    // instead of allocating a builder + providers on every publish.
    private static class DefaultMessageBuilder<T>
        where T : class
    {
        public static readonly IMessageBuilder<T> Instance = new MessageBuilder<T>(new DateTimeProvider(), new UnDashedUuidProvider());
    }
}
