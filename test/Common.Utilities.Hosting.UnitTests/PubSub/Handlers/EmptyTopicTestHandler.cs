using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class EmptyTopicTestHandler : BaseTestHandler
{
    public EmptyTopicTestHandler(HandlerDependency dependency)
        : base(dependency)
    {
    }

    [MessageHandler(topic: "")]
    public Task HandleAMessage(IMessage<object> message, CancellationToken cancellationToken)
    {
        Dependency.RegisterCall(nameof(EmptyTopicTestHandler), nameof(HandleAMessage), message.Topic);
        return Task.CompletedTask;
    }
}
