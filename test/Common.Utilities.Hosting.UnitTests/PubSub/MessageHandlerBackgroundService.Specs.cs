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
        [Fact]
        public async Task Should_Discover_Handlers()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<HandlerDependency>()
                .AddSingleton(new InProcessMessageHubOptions())
                .AddSingleton<IMessageHubAsync, InProcessMessageHub>();


            serviceCollection.AddPubSubMessageHandlers<MessageHandlerAttribute>(GetType().Assembly, att => att.Topic);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var host = serviceProvider.GetRequiredService<IHostedService>();
            var messageHub = serviceProvider.GetRequiredService<IMessageHubAsync>();
            var dependency = serviceProvider.GetRequiredService<HandlerDependency>();

            await host.StartAsync(CancellationToken.None);

            Assert.Equal(0, dependency.CountCall(nameof(TestHandler), nameof(TestHandler.HandleAMessage), "test-topic"));

            await messageHub.PublishAsync<object>("test-topic", null, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.Equal(1, dependency.CountCall(nameof(TestHandler), nameof(TestHandler.HandleAMessage), "test-topic"));

            await host.StopAsync(CancellationToken.None);
        }
    }
}
