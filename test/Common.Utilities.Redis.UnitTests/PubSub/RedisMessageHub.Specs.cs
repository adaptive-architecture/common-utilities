using AdaptArch.Common.Utilities.Redis.PubSub;
using Moq;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.PubSub;

public class RedisMessageHubSpecs
{
    private static readonly RedisMessageHubOptions Options = new();

    [Fact]
    public void Should_Throw_If_Arguments_Are_Null()
    {
        // ReSharper disable AssignNullToNotNullAttribute
        Assert.Throws<ArgumentNullException>(() => new RedisMessageHub(null, Options));
        Assert.Throws<ArgumentNullException>(() => new RedisMessageHub(null, null));
        // ReSharper restore AssignNullToNotNullAttribute
    }

    [Fact]
    public void Should_Subscribe_And_Unsubscribe_MessageHandler()
    {
        var handlerReactions = 0;
        Action<RedisChannel, RedisValue> handler = null;
        var shouldHaveRemovedHandler = false;
        var messageValue = RedisValue.EmptyString;

        var sub = new Mock<ISubscriber>();
        sub.Setup(s => s.Subscribe(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>())
            )
            .Callback((RedisChannel _, Action<RedisChannel, RedisValue> h, CommandFlags _) => handler = h);

        sub.Setup(s => s.Unsubscribe(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>())
            )
            .Callback((RedisChannel _, Action<RedisChannel, RedisValue> h, CommandFlags _) => shouldHaveRemovedHandler = handler == h);

        sub.Setup(s => s.Publish(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>())
            )
            .Callback((RedisChannel c, RedisValue val, CommandFlags _) =>
            {
                messageValue = val;
                handler.Invoke(c, val);
            });

        var cm = new Mock<IConnectionMultiplexer>();
        cm.Setup(s => s.GetSubscriber(It.Is<object>(arg => arg == null))).Returns(sub.Object);

        var hub = new RedisMessageHub(cm.Object, Options);

        var id = hub.Subscribe<object>("topic_A", (_, _) =>
        {
            handlerReactions++;
            return Task.CompletedTask;
        });

        // Check subscription
        Assert.NotNull(id);

        cm.Verify(v => v.GetSubscriber(null), Times.Once);
        sub.Verify(v => v.Subscribe("topic_A", handler, CommandFlags.None), Times.Once);


        Assert.Equal(0, handlerReactions);
        hub.Publish<object>("topic_A", "message");
        Assert.Equal(1, handlerReactions);

        // Check publish
        cm.Verify(v => v.GetSubscriber(null), Times.Exactly(2));
        sub.Verify(v => v.Publish("topic_A", messageValue, CommandFlags.None), Times.Once);

        // Unsubscribe in existent id
        hub.Unsubscribe("not=a=valid=id");

        // Check unsubscribe
        cm.Verify(v => v.GetSubscriber(null), Times.Exactly(2));
        sub.Verify(v => v.Unsubscribe("topic_A", handler, CommandFlags.None), Times.Never);


        // Unsubscribe
        hub.Unsubscribe(id);

        // Check unsubscribe
        cm.Verify(v => v.GetSubscriber(null), Times.Exactly(3));
        sub.Verify(v => v.Unsubscribe("topic_A", handler, CommandFlags.None), Times.Once);

        Assert.True(shouldHaveRemovedHandler);

        cm.VerifyNoOtherCalls();
        sub.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Should_Subscribe_And_Unsubscribe_MessageHandler_Async()
    {
        var handlerReactions = 0;
        Action<RedisChannel, RedisValue> handler = null;
        var shouldHaveRemovedHandler = false;
        var messageValue = RedisValue.EmptyString;

        var sub = new Mock<ISubscriber>();
        sub.Setup(s => s.SubscribeAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>())
            )
            .Callback((RedisChannel _, Action<RedisChannel, RedisValue> h, CommandFlags _) => handler = h);

        sub.Setup(s => s.UnsubscribeAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>())
            )
            .Callback((RedisChannel _, Action<RedisChannel, RedisValue> h, CommandFlags _) => shouldHaveRemovedHandler = handler == h);

        sub.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>())
            )
            .Callback((RedisChannel c, RedisValue val, CommandFlags _) =>
            {
                messageValue = val;
                handler.Invoke(c, val);
            });

        var cm = new Mock<IConnectionMultiplexer>();
        cm.Setup(s => s.GetSubscriber(It.Is<object>(arg => arg == null))).Returns(sub.Object);

        var hub = new RedisMessageHub(cm.Object, Options);

        var id = await hub.SubscribeAsync<object>("topic_A", (_, _) =>
        {
            handlerReactions++;
            return Task.CompletedTask;
        }, CancellationToken.None).ConfigureAwait(false);

        // Check subscription
        Assert.NotNull(id);

        cm.Verify(v => v.GetSubscriber(null), Times.Once);
        sub.Verify(v => v.SubscribeAsync("topic_A", handler, CommandFlags.None), Times.Once);


        Assert.Equal(0, handlerReactions);
        await hub.PublishAsync<object>("topic_A", "message", CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(1, handlerReactions);

        // Check publish
        cm.Verify(v => v.GetSubscriber(null), Times.Exactly(2));
        sub.Verify(v => v.PublishAsync("topic_A", messageValue, CommandFlags.None), Times.Once);

        // Unsubscribe in existent id
        await hub.UnsubscribeAsync("not=a=valid=id", CancellationToken.None).ConfigureAwait(false);

        // Check unsubscribe
        cm.Verify(v => v.GetSubscriber(null), Times.Exactly(2));
        sub.Verify(v => v.UnsubscribeAsync("topic_A", handler, CommandFlags.None), Times.Never);


        // Unsubscribe
        await hub.UnsubscribeAsync(id, CancellationToken.None).ConfigureAwait(false);

        // Check unsubscribe
        cm.Verify(v => v.GetSubscriber(null), Times.Exactly(3));
        sub.Verify(v => v.UnsubscribeAsync("topic_A", handler, CommandFlags.None), Times.Once);

        Assert.True(shouldHaveRemovedHandler);

        cm.VerifyNoOtherCalls();
        sub.VerifyNoOtherCalls();
    }
}
