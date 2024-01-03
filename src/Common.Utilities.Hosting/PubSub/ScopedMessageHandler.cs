using System.Reflection;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AdaptArch.Common.Utilities.Hosting.PubSub;

internal class ScopedMessageHandler<TMessage>
    where TMessage : class
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MethodInfo _handlerMethod;

    internal ScopedMessageHandler(IServiceScopeFactory scopeFactory, MethodInfo handlerMethod)
    {
        _scopeFactory = scopeFactory;
        _handlerMethod = handlerMethod;
    }

    internal async Task HandleAsync(IMessage<TMessage> message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlerImplementation = scope.ServiceProvider.GetService(_handlerMethod.DeclaringType!);

        var handlerInvocation = (Task)_handlerMethod.Invoke(handlerImplementation, [message, cancellationToken])!;

        await handlerInvocation.ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
    }
}
