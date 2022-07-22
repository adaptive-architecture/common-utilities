using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.UnitTests.PubSub;

public class MessageBuilderSpec
{
    [Fact]
    public void MessageBuilder_Should_Build_Messages()
    {
        var dateProvider = new DateTimeMockProvider(new[] {DateTime.UtcNow});
        var uuidProvider = new UuidMockProvider(new[] {Guid.NewGuid()});
        IMessageBuilder<object> messageBuilder = new MessageBuilder<object>(dateProvider, uuidProvider);

        var data = new { myProp = 1 };
        var message = messageBuilder.Build("my-topic", data);
        Assert.NotNull(message);
        Assert.Same(data, message.Data);
        Assert.Equal("my-topic", message.Topic);
        Assert.Equal(dateProvider.UtcNow, message.Timestamp);
        Assert.Equal(Guid.Parse(uuidProvider.New()), Guid.Parse(message.Id));
    }
}
