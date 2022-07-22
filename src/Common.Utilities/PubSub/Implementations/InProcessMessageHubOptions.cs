using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// Configuration options for <see cref="InProcessMessageHub"/>.
/// </summary>
public class InProcessMessageHubOptions
{
    /// <summary>
    /// Action to handle any exception thrown in the hub.
    /// </summary>
    public Action<Exception, IMessage<object>>? OnMessageHandlerError { get; set; }

    /// <summary>
    /// The hub's maximum degree of parallelism.
    /// This controls:
    ///  - How many handlers should be called in parallel as the result of a "publish".
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Accessor method for the message builder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public virtual IMessageBuilder<T> GetMessageBuilder<T>()
        where T : class
    {
        return new MessageBuilder<T>(new DateTimeProvider(), new UnDashedUuidProvider());
    }
}
