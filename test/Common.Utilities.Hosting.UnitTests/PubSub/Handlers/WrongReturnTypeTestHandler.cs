using System.Diagnostics;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class WrongReturnTypeTestHandler : BaseTestHandler
{
    public WrongReturnTypeTestHandler(HandlerDependency dependency)
        : base(dependency)
    {
    }

    [MessageHandler(topic: "test-topic")]
    public void HandleAMessage(IMessage<object> message, CancellationToken cancellationToken)
    {
        Debug.Print("Cancellation token: {0}", cancellationToken);
        Dependency.RegisterCall(nameof(WrongReturnTypeTestHandler), nameof(HandleAMessage), message.Topic);
    }
}
