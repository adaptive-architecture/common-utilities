using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.PubSub;

public class MessageBuilderSpecs
{
    [Fact]
    public void MessageBuilder_Should_Build_Messages()
    {
        var dateProvider = new DateTimeMockProvider([DateTime.UtcNow]);
        var uuidProvider = new UuidMockProvider([Guid.NewGuid()]);
        MessageBuilder<object> messageBuilder = new(dateProvider, uuidProvider);

        var data = new { myProp = 1 };
        var message = messageBuilder.Build("my-topic", data);
        Assert.NotNull(message);
        Assert.Same(data, message.Data);
        Assert.Equal("my-topic", message.Topic);
        Assert.Equal(dateProvider.UtcNow, message.Timestamp);
        Assert.Equal(Guid.Parse(uuidProvider.New()), Guid.Parse(message.Id));
    }
}
