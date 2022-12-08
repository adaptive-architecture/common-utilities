using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class TestHandler
{
    private readonly HandlerDependency _dependency;

    public TestHandler(HandlerDependency dependency)
    {
        _dependency = dependency;
    }

    [MessageHandler(topic: "test-topic")]
    public Task HandleAMessage(IMessage<object> message, CancellationToken cancellationToken)
    {
        _dependency.RegisterCall(nameof(TestHandler), nameof(HandleAMessage), message.Topic);
        return Task.CompletedTask;
    }
}
