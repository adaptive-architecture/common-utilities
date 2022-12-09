using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class WrongInputTypeTestHandler : BaseTestHandler
{
    public WrongInputTypeTestHandler(HandlerDependency dependency)
        : base(dependency)
    {
    }

    [MessageHandler(topic: "test-topic")]
    public Task NoParameters()
    {
        Dependency.RegisterCall(nameof(WrongInputTypeTestHandler), nameof(NoParameters), "test-topic");
        return Task.CompletedTask;
    }

    [MessageHandler(topic: "test-topic")]
    public Task TooManyParameters(IMessage<object> message, CancellationToken cancellationToken, object extraParam)
    {
        Dependency.RegisterCall(nameof(WrongInputTypeTestHandler), nameof(TooManyParameters), message.Topic);
        return Task.CompletedTask;
    }

    [MessageHandler(topic: "test-topic")]
    public Task WrongMessageType_1(object message, CancellationToken cancellationToken)
    {
        Dependency.RegisterCall(nameof(WrongInputTypeTestHandler), nameof(WrongMessageType_1), "test-topic");
        return Task.CompletedTask;
    }

    [MessageHandler(topic: "test-topic")]
    public Task WrongMessageType_2(IEnumerable<object> message, CancellationToken cancellationToken)
    {
        Dependency.RegisterCall(nameof(WrongInputTypeTestHandler), nameof(WrongMessageType_2), "test-topic");
        return Task.CompletedTask;
    }

    [MessageHandler(topic: "test-topic")]
    public Task WrongCancellationToken(IMessage<object> message, object cancellationToken)
    {
        Dependency.RegisterCall(nameof(WrongInputTypeTestHandler), nameof(WrongCancellationToken), message.Topic);
        return Task.CompletedTask;
    }
}
