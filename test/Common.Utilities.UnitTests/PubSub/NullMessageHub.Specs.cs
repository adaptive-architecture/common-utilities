using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.PubSub;

public class NullMessageHubSpecs
{
    private static NullMessageHub GetHub() => new(new NullMessageHubOptions());

    [Fact]
    public void Should_Accept_Valid_Options()
    {
        var options = new NullMessageHubOptions();
        var hub = new NullMessageHub(options);

        Assert.NotNull(hub);
    }

    [Fact]
    public void Should_Return_Subscription_Id_On_Subscribe()
    {
        var hub = GetHub();
        const string topic = "test-topic";

        var subscriptionId = hub.Subscribe<TestMessage>(topic, (_, _) => Task.CompletedTask);

        Assert.NotNull(subscriptionId);
        Assert.NotEmpty(subscriptionId);
    }

    [Fact]
    public void Should_Return_Same_Subscription_Id_For_Multiple_Subscriptions()
    {
        var hub = GetHub();
        const string topic = "test-topic";

        var subscriptionId1 = hub.Subscribe<TestMessage>(topic, (_, _) => Task.CompletedTask);
        var subscriptionId2 = hub.Subscribe<TestMessage>(topic, (_, _) => Task.CompletedTask);

        Assert.Equal(subscriptionId1, subscriptionId2);
    }

    [Fact]
    public void Should_Not_Invoke_Handler_On_Publish()
    {
        var hub = GetHub();
        const string topic = "test-topic";
        var handlerInvoked = false;

        hub.Subscribe<TestMessage>(topic, (_, _) =>
        {
            handlerInvoked = true;
            return Task.CompletedTask;
        });

        hub.Publish(topic, new TestMessage { Content = "test" });

        Assert.False(handlerInvoked);
    }

    [Fact]
    public void Should_Accept_Multiple_Publishes_Without_Error()
    {
        var hub = GetHub();
        const string topic = "test-topic";
        var message = new TestMessage { Content = "test" };

        hub.Publish(topic, message);
        hub.Publish(topic, message);
        hub.Publish(topic, message);

        // Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public void Should_Accept_Unsubscribe_Without_Error()
    {
        var hub = GetHub();
        var subscriptionId = hub.Subscribe<TestMessage>("test-topic", (_, _) => Task.CompletedTask);

        hub.Unsubscribe(subscriptionId);
        hub.Unsubscribe("non-existent-id");
        hub.Unsubscribe(null!);

        // Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Return_Subscription_Id_On_SubscribeAsync()
    {
        var hub = GetHub();
        const string topic = "test-topic";

        var subscriptionId = await hub.SubscribeAsync<TestMessage>(topic, (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.NotNull(subscriptionId);
        Assert.NotEmpty(subscriptionId);
    }

    [Fact]
    public async Task Should_Return_Same_Subscription_Id_For_Multiple_Async_Subscriptions()
    {
        var hub = GetHub();
        const string topic = "test-topic";

        var subscriptionId1 = await hub.SubscribeAsync<TestMessage>(topic, (_, _) => Task.CompletedTask, CancellationToken.None);
        var subscriptionId2 = await hub.SubscribeAsync<TestMessage>(topic, (_, _) => Task.CompletedTask, CancellationToken.None);

        Assert.Equal(subscriptionId1, subscriptionId2);
    }

    [Fact]
    public async Task Should_Not_Invoke_Handler_On_PublishAsync()
    {
        var hub = GetHub();
        const string topic = "test-topic";
        var handlerInvoked = false;

        await hub.SubscribeAsync<TestMessage>(topic, (_, _) =>
        {
            handlerInvoked = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        await hub.PublishAsync(topic, new TestMessage { Content = "test" }, CancellationToken.None);

        Assert.False(handlerInvoked);
    }

    [Fact]
    public async Task Should_Accept_Multiple_PublishAsync_Without_Error()
    {
        var hub = GetHub();
        const string topic = "test-topic";
        var message = new TestMessage { Content = "test" };

        await hub.PublishAsync(topic, message, CancellationToken.None);
        await hub.PublishAsync(topic, message, CancellationToken.None);
        await hub.PublishAsync(topic, message, CancellationToken.None);

        // Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Accept_UnsubscribeAsync_Without_Error()
    {
        var hub = GetHub();
        var subscriptionId = await hub.SubscribeAsync<TestMessage>("test-topic", (_, _) => Task.CompletedTask, CancellationToken.None);

        await hub.UnsubscribeAsync(subscriptionId, CancellationToken.None);
        await hub.UnsubscribeAsync("non-existent-id", CancellationToken.None);
        await hub.UnsubscribeAsync(null!, CancellationToken.None);

        // Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task Should_Handle_Cancellation_Token_In_Async_Methods()
    {
        var hub = GetHub();
        const string topic = "test-topic";
        var message = new TestMessage { Content = "test" };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Methods should complete immediately and not throw for cancelled tokens
        var subscriptionId = await hub.SubscribeAsync<TestMessage>(topic, (_, _) => Task.CompletedTask, cts.Token);
        await hub.PublishAsync(topic, message, cts.Token);
        await hub.UnsubscribeAsync(subscriptionId, cts.Token);

        Assert.NotNull(subscriptionId);
    }

    [Fact]
    public void Should_Handle_Null_And_Empty_Topics()
    {
        var hub = GetHub();
        var message = new TestMessage { Content = "test" };

        // Should not throw for null/empty topics
        hub.Subscribe<TestMessage>(null!, (_, _) => Task.CompletedTask);
        hub.Subscribe<TestMessage>(String.Empty, (_, _) => Task.CompletedTask);
        hub.Publish(null!, message);
        hub.Publish(String.Empty, message);

        Assert.True(true);
    }

    [Fact]
    public async Task Should_Handle_Null_And_Empty_Topics_Async()
    {
        var hub = GetHub();
        var message = new TestMessage { Content = "test" };

        // Should not throw for null/empty topics
        await hub.SubscribeAsync<TestMessage>(null!, (_, _) => Task.CompletedTask, CancellationToken.None);
        await hub.SubscribeAsync<TestMessage>(String.Empty, (_, _) => Task.CompletedTask, CancellationToken.None);
        await hub.PublishAsync(null!, message, CancellationToken.None);
        await hub.PublishAsync(String.Empty, message, CancellationToken.None);

        Assert.True(true);
    }

    private class TestMessage
    {
        public string Content { get; set; } = String.Empty;
    }
}
