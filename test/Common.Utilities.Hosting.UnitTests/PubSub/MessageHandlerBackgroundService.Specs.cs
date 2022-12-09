using AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceCollection;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub
{
    public class UnitMessageHandlerBackgroundServiceSpecs
    {
        private readonly ServiceCollection _serviceCollection;
        private readonly Lazy<IServiceProvider> _serviceProviderSingleton;
        public UnitMessageHandlerBackgroundServiceSpecs()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection
                .AddSingleton<HandlerDependency>()
                .AddSingleton(new InProcessMessageHubOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 4
                })
                .AddSingleton<IMessageHubAsync, InProcessMessageHub>()
                .AddPubSubMessageHandlers<MessageHandlerAttribute>(GetType().Assembly, att => att.Topic);


            _serviceProviderSingleton = new Lazy<IServiceProvider>(BuildServiceProvider);
        }

        private IServiceProvider BuildServiceProvider() => _serviceCollection.BuildServiceProvider();
        private IServiceProvider ServiceProvider => _serviceProviderSingleton.Value;

        private Task StartHostsAsync() => Parallel.ForEachAsync(
            ServiceProvider.GetServices<IHostedService>(),
            CancellationToken.None,
            async (host, ct) => await host.StartAsync(ct).ConfigureAwait(false));

        private Task StopHostsAsync() => Parallel.ForEachAsync(
            ServiceProvider.GetServices<IHostedService>(),
            CancellationToken.None,
            async (host, ct) => await host.StopAsync(ct).ConfigureAwait(false));

        private void VerifyCount(int count, string @class, string method, string topic)
        {
            var dependency = ServiceProvider.GetRequiredService<HandlerDependency>();
            Assert.Equal(count, dependency.CountCall(@class, method, topic));
        }

        private async Task PublishAsync(string topic)
        {
            var messageHub = ServiceProvider.GetRequiredService<IMessageHubAsync>();
            await messageHub.PublishAsync(topic, new object(), CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_Discover_Handlers_And_Call_Them()
        {
            await StartHostsAsync().ConfigureAwait(false);

            // Ensure we have no calls.
            VerifyCount(0, nameof(TestHandler), nameof(TestHandler.HandleAMessage), "test-topic");

            await PublishAsync("test-topic").ConfigureAwait(false);
            // Ensure we have 1 call.
            VerifyCount(1, nameof(TestHandler), nameof(TestHandler.HandleAMessage), "test-topic");

            await StopHostsAsync().ConfigureAwait(false);

            await PublishAsync("test-topic").ConfigureAwait(false);
            // Ensure we have 1 call.
            // The subscription should have been cancelled as the service stopped.
            VerifyCount(1, nameof(TestHandler), nameof(TestHandler.HandleAMessage), "test-topic");
        }

        [Fact]
        public async Task Should_Discover_Handlers_Multiple_Times_And_Call_Them()
        {
            var testTopics = new[]
            {
                "test-topic-1",
                "test-topic-2",
                "test-topic-3"
            };

            await StartHostsAsync().ConfigureAwait(false);

            foreach (var testTopic in testTopics)
            {
                await PublishAsync(testTopic).ConfigureAwait(false);
                VerifyCount(0, nameof(EmptyTopicTestHandler), nameof(EmptyTopicTestHandler.HandleAMessage), testTopic);
            }

            await StopHostsAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_Discover_Ignore_Missing_Topics()
        {
            await StartHostsAsync().ConfigureAwait(false);

            await PublishAsync("test-topic").ConfigureAwait(false);
            // Ensure we have 0 calls.
            VerifyCount(0, nameof(EmptyTopicTestHandler), nameof(EmptyTopicTestHandler.HandleAMessage), "test-topic");

            await StopHostsAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_Discover_Ignore_Wrong_Return_Type()
        {
            await StartHostsAsync().ConfigureAwait(false);

            await PublishAsync("test-topic").ConfigureAwait(false);
            // Ensure we have 0 calls.
            VerifyCount(0, nameof(WrongReturnTypeTestHandler), nameof(WrongReturnTypeTestHandler.HandleAMessage), "test-topic");

            await StopHostsAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_Discover_Ignore_Wrong_Return_Input()
        {
            var methodsToCheck = new[]
            {
                nameof(WrongInputTypeTestHandler.NoParameters),
                nameof(WrongInputTypeTestHandler.TooManyParameters),
                nameof(WrongInputTypeTestHandler.WrongMessageType_1),
                nameof(WrongInputTypeTestHandler.WrongMessageType_2),
                nameof(WrongInputTypeTestHandler.WrongCancellationToken)
            };
            await StartHostsAsync().ConfigureAwait(false);

            await PublishAsync("test-topic").ConfigureAwait(false);
            // Ensure we have 0 calls.

            foreach (var method in methodsToCheck)
            {
                VerifyCount(0, nameof(WrongInputTypeTestHandler), method, "test-topic");
            }

            await StopHostsAsync().ConfigureAwait(false);
        }
    }
}
