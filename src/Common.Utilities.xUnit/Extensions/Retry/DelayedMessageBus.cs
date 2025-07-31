/*
   Based on https://github.com/xunit/samples.xunit/blob/28d3683f74b104d33544efe5d1ae45ce9b0ad8c5/v3/RetryFactExample/
*/
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

/// <summary>
/// Used to capture messages to potentially be forwarded later. Messages are forwarded by
/// disposing of the message bus.
/// </summary>
public sealed class DelayedMessageBus(IMessageBus innerBus) : IMessageBus
{
    readonly List<IMessageSinkMessage> messages = [];

#pragma warning disable S2325 // SONAR static method false positive
    /// <inheritdoc/>
    public bool QueueMessage(IMessageSinkMessage message)
    {
#pragma warning restore S2325
        // Technically speaking, this lock isn't necessary in our case, because we know we're using this
        // message bus for a single test (so there's no possibility of parallelism). However, it's good
        // practice when something might be used where parallel messages might arrive, so it's here in
        // this sample.
        lock (messages)
            messages.Add(message);

        // No way to ask the inner bus if they want to cancel without sending them the message, so
        // we just go ahead and continue always.
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var message in messages)
            innerBus.QueueMessage(message);
    }
}
