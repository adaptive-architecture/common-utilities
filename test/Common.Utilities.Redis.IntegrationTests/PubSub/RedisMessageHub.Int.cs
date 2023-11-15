using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.Redis.PubSub;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests.PubSub;

public class RedisMessageHubInt
{
    private static readonly TimeSpan WaitTime = TimeSpan.FromMilliseconds(100);
    public record MyMessage
    {
        public string Id { get; set; }
    }

    private readonly IMessageHub _messageHub;
    private readonly IMessageHubAsync _messageHubAsync;

    public RedisMessageHubInt()
    {
        var hub = new RedisMessageHub(Utilities.GetDefaultConnectionMultiplexer(), new RedisMessageHubOptions());
        _messageHub = hub;
        _messageHubAsync = hub;
    }

    [Fact]
    public void Should_Subscribe_Publish_Unsubscribe()
    {
        MyMessage receivedMessage = null;
        var subId = _messageHub.Subscribe<MyMessage>(nameof(MyMessage),
            (m, _) =>
            {
                receivedMessage = m.Data;
                return Task.CompletedTask;
            });

        Assert.Null(receivedMessage);

        var sentMessage = new MyMessage { Id = Guid.NewGuid().ToString("N") };

        _messageHub.Publish(nameof(MyMessage), sentMessage);
        Thread.Sleep(WaitTime);

        Assert.NotNull(receivedMessage);
        Assert.Equal(sentMessage.Id, receivedMessage.Id);

        _messageHub.Unsubscribe(subId);
        receivedMessage = null;

        _messageHub.Publish(nameof(MyMessage), sentMessage);
        Thread.Sleep(WaitTime);

        Assert.Null(receivedMessage);
    }

    [Fact]
    public async Task Should_Subscribe_Publish_Unsubscribe_Async()
    {
        MyMessage receivedMessage = null;
        var subId = await _messageHubAsync.SubscribeAsync<MyMessage>(nameof(MyMessage),
                (m, _) =>
                {
                    receivedMessage = m.Data;
                    return Task.CompletedTask;
                }, CancellationToken.None)
            ;

        Assert.Null(receivedMessage);

        var sentMessage = new MyMessage { Id = Guid.NewGuid().ToString("N") };

        await _messageHubAsync.PublishAsync(nameof(MyMessage), sentMessage, CancellationToken.None)
            ;

        await Task.Delay(WaitTime);

        Assert.NotNull(receivedMessage);
        Assert.Equal(sentMessage.Id, receivedMessage.Id);

        await _messageHubAsync.UnsubscribeAsync(subId, CancellationToken.None);
        receivedMessage = null;

        await _messageHubAsync.PublishAsync(nameof(MyMessage), sentMessage, CancellationToken.None)
            ;

        await Task.Delay(WaitTime);

        Assert.Null(receivedMessage);
    }
}
