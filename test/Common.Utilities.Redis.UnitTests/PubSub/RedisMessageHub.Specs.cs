using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.Redis.PubSub;
using AdaptArch.Common.Utilities.Redis.Utilities;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.PubSub;

public class RedisMessageHubSpecs
{
    private static readonly RedisMessageHubOptions Options = new();

    private int _handlerReactions;
    private Action<RedisChannel, RedisValue> _handler;
    private bool _shouldHaveRemovedHandler;
    private RedisValue _messageValue = RedisValue.EmptyString;

    private readonly ISubscriber _sub;
    private readonly IConnectionMultiplexer _cm;
    private readonly RedisMessageHub _hub;

    public RedisMessageHubSpecs()
    {
        _sub = Substitute.For<ISubscriber>();

        _sub.When(s => s.Subscribe(Arg.Any<RedisChannel>(), Arg.Any<Action<RedisChannel, RedisValue>>(),
                Arg.Any<CommandFlags>()))
            .Do(SubscribeCallback());

        _sub.When(s => s.SubscribeAsync(Arg.Any<RedisChannel>(), Arg.Any<Action<RedisChannel, RedisValue>>(),
                Arg.Any<CommandFlags>()))
            .Do(SubscribeCallback());

        _sub.When(s => s.Unsubscribe(Arg.Any<RedisChannel>(), Arg.Any<Action<RedisChannel, RedisValue>>(),
                Arg.Any<CommandFlags>()))
            .Do(UnsubscribeCallback());
        _sub.When(s => s.UnsubscribeAsync(Arg.Any<RedisChannel>(), Arg.Any<Action<RedisChannel, RedisValue>>(),
                Arg.Any<CommandFlags>()))
            .Do(UnsubscribeCallback());

        _sub.When(s => s.Publish(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>()))
            .Do(PublishCallback());
        _sub.When(s => s.PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>()))
            .Do(PublishCallback());

        _cm = Substitute.For<IConnectionMultiplexer>();
        _ = _cm.GetSubscriber(null).Returns(_sub);

        _hub = new RedisMessageHub(_cm, Options);

        Action<NSubstitute.Core.CallInfo> SubscribeCallback()
        {
            return (args) => _handler = args.ArgAt<Action<RedisChannel, RedisValue>>(1);
        }

        Action<NSubstitute.Core.CallInfo> UnsubscribeCallback()
        {
            return (args) => _shouldHaveRemovedHandler = _handler == args.ArgAt<Action<RedisChannel, RedisValue>>(1);
        }

        Action<NSubstitute.Core.CallInfo> PublishCallback()
        {
            return (args) =>
            {
                _messageValue = args.ArgAt<RedisValue>(1);
                _handler!.Invoke(args.ArgAt<RedisChannel>(0), _messageValue);
            };
        }
    }

    [Fact]
    public void Should_Throw_If_Arguments_Are_Null()
    {
        // ReSharper disable AssignNullToNotNullAttribute
        _ = Assert.Throws<ArgumentNullException>(() => new RedisMessageHub(null, Options));
        _ = Assert.Throws<ArgumentNullException>(() => new RedisMessageHub(null, null));
        // ReSharper restore AssignNullToNotNullAttribute
    }

    private Task TopicHandler(IMessage<object> msg, CancellationToken ct)
    {
        _handlerReactions++;
        return Task.CompletedTask;
    }

    private void VerifySubscriberCalled(int times) => _cm.ReceivedWithAnyArgs(times).GetSubscriber(null);

    [Fact]
    public void Should_Subscribe_And_Unsubscribe_MessageHandler()
    {
        VerifySubscriberCalled(0);
        var id = _hub.Subscribe<object>("topic_A", TopicHandler);

        // Check subscription
        Assert.NotNull(id);

        VerifySubscriberCalled(1);
        _sub.Received(1).Subscribe("topic_A".ToChannel(), _handler, CommandFlags.None);

        Assert.Equal(0, _handlerReactions);
        _hub.Publish<object>("topic_A", "message");
        Assert.Equal(1, _handlerReactions);

        // Check publish
        VerifySubscriberCalled(2);
        _ = _sub.Received(1).Publish("topic_A".ToChannel(), _messageValue, CommandFlags.None);

        // Unsubscribe in existent id
        _hub.Unsubscribe("not=a=valid=id");

        // Check unsubscribe
        VerifySubscriberCalled(2);
        _sub.DidNotReceive().Unsubscribe("topic_A".ToChannel(), _handler, CommandFlags.None);

        // Unsubscribe
        _hub.Unsubscribe(id);

        // Check unsubscribe
        VerifySubscriberCalled(3);
        _sub.Received(1).Unsubscribe("topic_A".ToChannel(), _handler, CommandFlags.None);

        Assert.True(_shouldHaveRemovedHandler);

        Assert.Equal(3, _cm.ReceivedCalls().Count());
        Assert.Equal(3, _sub.ReceivedCalls().Count());
    }

    [Fact]
    public async Task Should_Subscribe_And_Unsubscribe_MessageHandler_Async()
    {
        VerifySubscriberCalled(0);
        var id = await _hub.SubscribeAsync<object>("topic_A", TopicHandler, CancellationToken.None);

        // Check subscription
        Assert.NotNull(id);

        VerifySubscriberCalled(1);
        await _sub.Received(1).SubscribeAsync("topic_A".ToChannel(), _handler, CommandFlags.None);

        Assert.Equal(0, _handlerReactions);
        await _hub.PublishAsync<object>("topic_A", "message", CancellationToken.None);
        Assert.Equal(1, _handlerReactions);

        // Check publish
        VerifySubscriberCalled(2);
        _ = await _sub.Received(1).PublishAsync("topic_A".ToChannel(), _messageValue, CommandFlags.None);

        // Unsubscribe in existent id
        await _hub.UnsubscribeAsync("not=a=valid=id", CancellationToken.None);

        // Check unsubscribe
        VerifySubscriberCalled(2);
        await _sub.DidNotReceive().UnsubscribeAsync("topic_A".ToChannel(), _handler, CommandFlags.None);

        // Unsubscribe
        await _hub.UnsubscribeAsync(id, CancellationToken.None);

        // Check unsubscribe
        VerifySubscriberCalled(3);
        await _sub.Received(1).UnsubscribeAsync("topic_A".ToChannel(), _handler, CommandFlags.None);

        Assert.True(_shouldHaveRemovedHandler);

        Assert.Equal(3, _cm.ReceivedCalls().Count());
        Assert.Equal(3, _sub.ReceivedCalls().Count());
    }
}
