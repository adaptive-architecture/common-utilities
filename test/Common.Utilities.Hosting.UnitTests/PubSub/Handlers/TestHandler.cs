﻿using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class TestHandler : BaseTestHandler
{
    public TestHandler(HandlerDependency dependency)
        : base(dependency)
    {
    }

    [MessageHandler(topic: "test-topic")]
    public Task HandleAMessage(IMessage<object> message, CancellationToken cancellationToken)
    {
        Dependency.RegisterCall(nameof(TestHandler), nameof(HandleAMessage), message.Topic);
        return Task.CompletedTask;
    }
}
