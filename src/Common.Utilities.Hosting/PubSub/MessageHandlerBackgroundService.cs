using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdaptArch.Common.Utilities.Hosting.PubSub;

[RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
[RequiresUnreferencedCode("Calls methods from the \"System.Reflection\" namespace.")]
internal class MessageHandlerBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _subscriptionIds = new();
    private readonly IMessageHubAsync _messageHub;
    private readonly IReadOnlyCollection<HandlerDefinitions> _handlerDefinitions;

    public MessageHandlerBackgroundService(IServiceProvider serviceProvider, IReadOnlyCollection<HandlerDefinitions> handlerDefinitions)
    {
        _serviceProvider = serviceProvider;
        _handlerDefinitions = handlerDefinitions;
        _messageHub = _serviceProvider.GetRequiredService<IMessageHubAsync>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var messageHandlerType = typeof(MessageHandler<>);
        var subscribeAsyncMethod = typeof(IMessageHubAsync)
            .GetMethod(nameof(IMessageHubAsync.SubscribeAsync), BindingFlags.Instance | BindingFlags.Public);

        foreach (var handlerDefinition in _handlerDefinitions)
        {
            var messageType = handlerDefinition.HandlerMethod.GetParameters()[0].ParameterType.GetGenericArguments()[0];
            var delegateType = messageHandlerType.MakeGenericType(messageType);

            var scopedHandlerInstance = ScopedMessageHandlerCreator
                .CreateScopedMessageHandlerDynamically(messageType,
                    _serviceProvider.GetRequiredService<IServiceScopeFactory>(), handlerDefinition.HandlerMethod);

            var handler = Delegate.CreateDelegate(delegateType, scopedHandlerInstance, nameof(ScopedMessageHandler<object>.HandleAsync));

            var genericType = subscribeAsyncMethod!.MakeGenericMethod(messageType);
            var resultTask = (Task<string>)genericType.Invoke(_messageHub,
            [
                handlerDefinition.Topic, handler, cancellationToken
            ])!;

            _subscriptionIds.Add(await resultTask.ConfigureAwait(false));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(_subscriptionIds,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                },
                async (id, ct) => await _messageHub.UnsubscribeAsync(id, ct).ConfigureAwait(false))
            .ConfigureAwait(false);
    }
}
