using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

namespace AdaptArch.Common.Utilities.UnitTests.PubSub;
#pragma warning disable S2925 // SONAR: Do not use 'Thread.Sleep()' in a test.

public class InProcessMessageHubSpecs
{
    private static InProcessMessageHub GetHub() => new(new InProcessMessageHubOptions());

    [Fact]
    public void Constructor_Should_Throw_If_Null_Options()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new InProcessMessageHub(null));
    }

    [RetryFact]
    public void Should_Subscribe_React_And_Unsubscribe()
    {
        var topic = Guid.NewGuid().ToString("N");
        var reacted = 0;
        var hub = GetHub();

        var subscriptionId = hub.Subscribe<object>(topic, (_, _) =>
        {
            reacted++;
            return Task.CompletedTask;
        });

        hub.Publish<object>(topic, null);
        hub.Publish<object>("other-topic", null);

        Thread.Sleep(TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, reacted);

        hub.Unsubscribe(subscriptionId);

        hub.Publish<object>(topic, null);

        Thread.Sleep(TimeSpan.FromMilliseconds(10));
        Assert.Equal(1, reacted);
    }

    [RetryFact]
    public async Task Should_Subscribe_React_And_Unsubscribe_Async()
    {
        var topic = Guid.NewGuid().ToString("N");
        var reacted = 0;
        var hub = GetHub();

        var subscriptionId = await hub.SubscribeAsync<object>(topic, (_, _) =>
        {
            reacted++;
            return Task.CompletedTask;
        }, CancellationToken.None);

        await hub.PublishAsync<object>(topic, null, CancellationToken.None);
        await hub.PublishAsync<object>("other-topic", null, CancellationToken.None);

        Assert.Equal(1, reacted);

        await hub.UnsubscribeAsync(subscriptionId, CancellationToken.None);

        await hub.PublishAsync<object>(topic, null, CancellationToken.None);

        Assert.Equal(1, reacted);
    }

    [Fact]
    public void Should_Be_Able_To_Subscribe_Multiple_Times()
    {
        var topic = Guid.NewGuid().ToString("N");
        var reacted = 0;
        var hub = GetHub();

        _ = hub.Subscribe<object>(topic, (_, _) =>
        {
            reacted++;
            return Task.CompletedTask;
        });

        _ = hub.Subscribe<object>(topic, (_, _) =>
        {
            reacted++;
            return Task.CompletedTask;
        });

        _ = hub.Subscribe<object>(topic, (_, _) =>
        {
            reacted++;
            return Task.CompletedTask;
        });

        hub.Publish<object>(topic, null);

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        Assert.Equal(3, reacted);
    }

    [Fact]
    public void Should_Not_Fail_If_Handler_Failed()
    {
        var topic = Guid.NewGuid().ToString("N");
        const int reacted = 0;
        var hub = GetHub();

        _ = hub.Subscribe<object>(topic, (_, _) => throw new ApplicationException());

        hub.Publish<object>(topic, null);

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        Assert.Equal(0, reacted);
    }

    [RetryFact]
    public void Should_Intercept_Exception()
    {
        var topic = Guid.NewGuid().ToString("N");
        var reacted = 0;
        var hub = new InProcessMessageHub(new InProcessMessageHubOptions { OnMessageHandlerError = (_, _) => reacted++ });

        _ = hub.Subscribe<object>(topic, (_, _) => throw new ApplicationException());

        hub.Publish<object>(topic, null);

        Thread.Sleep(TimeSpan.FromMilliseconds(100));

        var attempts = 40;
        while (attempts-- > 0)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(20));
            if (reacted > 0)
            {
                break;
            }
        }

        Assert.Equal(1, reacted);
    }

    [Fact]
    public void Should_Intercept_Exception_And_Not_Fail_If_Error_Handler_Fails()
    {
        var topic = Guid.NewGuid().ToString("N");
        var reacted = 0;
        var hub = new InProcessMessageHub(new InProcessMessageHubOptions
        {
            OnMessageHandlerError = (_, _) =>
            {
                reacted++;
                throw new ApplicationException();
            }
        });

        _ = hub.Subscribe<object>(topic, (_, _) => throw new ApplicationException());

        hub.Publish<object>(topic, null);

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        Assert.Equal(1, reacted);
    }
}
