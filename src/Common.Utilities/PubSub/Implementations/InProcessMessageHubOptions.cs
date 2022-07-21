using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

public class InProcessMessageHubOptions
{
    public Action<Exception, IMessage<object>>? OnMessageHandlerError { get; set; }

    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    public IMessageBuilder<T> GetMessageBuilder<T>()
        where T : class
    {
        return new MessageBuilder<T>();
    }
}

