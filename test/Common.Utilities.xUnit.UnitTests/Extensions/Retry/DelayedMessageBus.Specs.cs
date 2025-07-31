using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using NSubstitute;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class DelayedMessageBusSpecs
{
    [Fact]
    public void QueueMessage_ShouldAddMessageToInternalList()
    {
        var innerBus = Substitute.For<IMessageBus>();
        var delayedBus = new DelayedMessageBus(innerBus);
        var message = Substitute.For<IMessageSinkMessage>();

        var result = delayedBus.QueueMessage(message);

        Assert.True(result);
        innerBus.DidNotReceive().QueueMessage(Arg.Any<IMessageSinkMessage>());
    }

    [Fact]
    public void QueueMessage_WithMultipleMessages_ShouldAddAllMessagesToInternalList()
    {
        var innerBus = Substitute.For<IMessageBus>();
        var delayedBus = new DelayedMessageBus(innerBus);
        var message1 = Substitute.For<IMessageSinkMessage>();
        var message2 = Substitute.For<IMessageSinkMessage>();

        delayedBus.QueueMessage(message1);
        delayedBus.QueueMessage(message2);

        innerBus.DidNotReceive().QueueMessage(Arg.Any<IMessageSinkMessage>());
    }

    [Fact]
    public void Dispose_ShouldForwardAllMessagesToInnerBus()
    {
        var innerBus = Substitute.For<IMessageBus>();
        var delayedBus = new DelayedMessageBus(innerBus);
        var message1 = Substitute.For<IMessageSinkMessage>();
        var message2 = Substitute.For<IMessageSinkMessage>();

        delayedBus.QueueMessage(message1);
        delayedBus.QueueMessage(message2);
        delayedBus.Dispose();

        Received.InOrder(() =>
        {
            innerBus.QueueMessage(message1);
            innerBus.QueueMessage(message2);
        });
    }

    [Fact]
    public void Dispose_WithNoMessages_ShouldNotCallInnerBus()
    {
        var innerBus = Substitute.For<IMessageBus>();
        var delayedBus = new DelayedMessageBus(innerBus);

        delayedBus.Dispose();

        innerBus.DidNotReceive().QueueMessage(Arg.Any<IMessageSinkMessage>());
    }

    [Fact]
    public void QueueMessage_AfterDispose_ShouldStillWork()
    {
        var innerBus = Substitute.For<IMessageBus>();
        var delayedBus = new DelayedMessageBus(innerBus);
        var message1 = Substitute.For<IMessageSinkMessage>();
        var message2 = Substitute.For<IMessageSinkMessage>();

        delayedBus.QueueMessage(message1);
        delayedBus.Dispose();
        delayedBus.QueueMessage(message2);

        innerBus.Received(1).QueueMessage(message1);
        innerBus.DidNotReceive().QueueMessage(message2);
    }

    [Fact]
    public void Constructor_WithNullInnerBus_ShouldNotThrow()
    {
        var exception = Record.Exception(() => new DelayedMessageBus(null!));
        Assert.Null(exception);
    }
}
