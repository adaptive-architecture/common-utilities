using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.Redis.PubSub;
using Moq;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.PubSub;

public class RedisMessageHubSpecs
{
    private static readonly RedisMessageHubOptions Options = new();

    private int _handlerReactions;
    private Action<RedisChannel, RedisValue> _handler;
    private bool _shouldHaveRemovedHandler;
    private RedisValue _messageValue = RedisValue.EmptyString;

    private readonly Mock<ISubscriber> _sub;
    private readonly Mock<IConnectionMultiplexer> _cm;
    private readonly RedisMessageHub _hub;

    public RedisMessageHubSpecs()
    {
        Action<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags> SubscribeCallback()
        {
            return (_, h, _) => _handler = h;
        }

        Action<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags> UnsubscribeCallback()
        {
            return (_, h, _) => _shouldHaveRemovedHandler = _handler == h;
        }

        Action<RedisChannel, RedisValue, CommandFlags> PublishCallback()
        {
            return (c, val, _) =>
            {
                _messageValue = val;
                _handler!.Invoke(c, val);
            };
        }

        _sub = new Mock<ISubscriber>();

        _sub.Setup(s => s.Subscribe(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback(SubscribeCallback());
        _sub.Setup(s => s.SubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback(SubscribeCallback());

        _sub.Setup(s => s.Unsubscribe(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback(UnsubscribeCallback());
        _sub.Setup(s => s.UnsubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback(UnsubscribeCallback());

        _sub.Setup(s => s.Publish(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback(PublishCallback());
        _sub.Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback(PublishCallback());

        _cm = new Mock<IConnectionMultiplexer>();
        _cm.Setup(s => s.GetSubscriber(It.Is<object>(arg => arg == null))).Returns(_sub.Object);

        _hub = new RedisMessageHub(_cm.Object, Options);
    }

    [Fact]
    public void Should_Throw_If_Arguments_Are_Null()
    {
        // ReSharper disable AssignNullToNotNullAttribute
        Assert.Throws<ArgumentNullException>(() => new RedisMessageHub(null, Options));
        Assert.Throws<ArgumentNullException>(() => new RedisMessageHub(null, null));
        // ReSharper restore AssignNullToNotNullAttribute
    }

    private Task TopicHandler(IMessage<object> msg, CancellationToken ct)
    {
        _handlerReactions++;
        return Task.CompletedTask;
    }

    private void VerifySubscriberCalled(int times) => _cm.Verify(v => v.GetSubscriber(null), Times.Exactly(times));

    [Fact]
    public void Should_Subscribe_And_Unsubscribe_MessageHandler()
    {
        VerifySubscriberCalled(0);
        var id = _hub.Subscribe<object>("topic_A", TopicHandler);

        // Check subscription
        Assert.NotNull(id);

        VerifySubscriberCalled(1);
        _sub.Verify(v => v.Subscribe("topic_A", _handler, CommandFlags.None), Times.Once);

        Assert.Equal(0, _handlerReactions);
        _hub.Publish<object>("topic_A", "message");
        Assert.Equal(1, _handlerReactions);

        // Check publish
        VerifySubscriberCalled(2);
        _sub.Verify(v => v.Publish("topic_A", _messageValue, CommandFlags.None), Times.Once);

        // Unsubscribe in existent id
        _hub.Unsubscribe("not=a=valid=id");

        // Check unsubscribe
        VerifySubscriberCalled(2);
        _sub.Verify(v => v.Unsubscribe("topic_A", _handler, CommandFlags.None), Times.Never);

        // Unsubscribe
        _hub.Unsubscribe(id);

        // Check unsubscribe
        VerifySubscriberCalled(3);
        _sub.Verify(v => v.Unsubscribe("topic_A", _handler, CommandFlags.None), Times.Once);

        Assert.True(_shouldHaveRemovedHandler);

        _cm.VerifyNoOtherCalls();
        _sub.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Should_Subscribe_And_Unsubscribe_MessageHandler_Async()
    {
        VerifySubscriberCalled(0);
        var id = await _hub.SubscribeAsync<object>("topic_A", TopicHandler, CancellationToken.None).ConfigureAwait(false);

        // Check subscription
        Assert.NotNull(id);

        VerifySubscriberCalled(1);
        _sub.Verify(v => v.SubscribeAsync("topic_A", _handler, CommandFlags.None), Times.Once);

        Assert.Equal(0, _handlerReactions);
        await _hub.PublishAsync<object>("topic_A", "message", CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(1, _handlerReactions);

        // Check publish
        VerifySubscriberCalled(2);
        _sub.Verify(v => v.PublishAsync("topic_A", _messageValue, CommandFlags.None), Times.Once);

        // Unsubscribe in existent id
        await _hub.UnsubscribeAsync("not=a=valid=id", CancellationToken.None).ConfigureAwait(false);

        // Check unsubscribe
        VerifySubscriberCalled(2);
        _sub.Verify(v => v.UnsubscribeAsync("topic_A", _handler, CommandFlags.None), Times.Never);

        // Unsubscribe
        await _hub.UnsubscribeAsync(id, CancellationToken.None).ConfigureAwait(false);

        // Check unsubscribe
        VerifySubscriberCalled(3);
        _sub.Verify(v => v.UnsubscribeAsync("topic_A", _handler, CommandFlags.None), Times.Once);

        Assert.True(_shouldHaveRemovedHandler);

        _cm.VerifyNoOtherCalls();
        _sub.VerifyNoOtherCalls();
    }
}
