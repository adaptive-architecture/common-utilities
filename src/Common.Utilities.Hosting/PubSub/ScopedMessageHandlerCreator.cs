using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AdaptArch.Common.Utilities.Hosting.PubSub;

internal static class ScopedMessageHandlerCreator
{
    public static ScopedMessageHandler<T> CreateScopedMessageHandler<T>(IServiceScopeFactory scopeFactory,
        MethodInfo handlerMethod) where T : class => new(scopeFactory, handlerMethod);

    [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
    [RequiresUnreferencedCode("Calls methods from the \"System.Reflection\" namespace.")]
    public static object CreateScopedMessageHandlerDynamically(Type type, IServiceScopeFactory scopeFactory,
        MethodInfo handlerMethod)
    {
        return typeof(ScopedMessageHandlerCreator)
            .GetMethod(nameof(CreateScopedMessageHandler))!
            .MakeGenericMethod(type)
            .Invoke(null, [scopeFactory, handlerMethod])!;
    }
}
