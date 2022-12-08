using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AdaptArch.Common.Utilities.Hosting.PubSub;

internal static class ScopedMessageHandlerCreator
{
    public static ScopedMessageHandler<T> CreateScopedMessageHandler<T>(IServiceScopeFactory scopeFactory,
        MethodInfo handlerMethod) where T : class => new(scopeFactory, handlerMethod);

    public static object CreateScopedMessageHandlerDynamically(Type type, IServiceScopeFactory scopeFactory,
        MethodInfo handlerMethod)
    {
        return typeof(ScopedMessageHandlerCreator)
            .GetMethod(nameof(CreateScopedMessageHandler))!
            .MakeGenericMethod(type)
            .Invoke(null, new object[] { scopeFactory, handlerMethod })!;
    }
}
