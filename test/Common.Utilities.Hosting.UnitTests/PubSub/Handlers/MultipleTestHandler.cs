using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class MultipleTestHandler : BaseTestHandler
{
    public MultipleTestHandler(HandlerDependency dependency)
        : base(dependency)
    {
    }

    [MessageHandler(topic: "test-topic-1")]
    [MessageHandler(topic: "test-topic-2")]
    public Task HandleAMessage(IMessage<object> message, CancellationToken cancellationToken)
    {
        Dependency.RegisterCall(nameof(MultipleTestHandler), nameof(HandleAMessage), message.Topic);
        return Task.CompletedTask;
    }
}
