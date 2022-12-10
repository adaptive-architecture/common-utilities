using System.Reflection;
using AdaptArch.Common.Utilities.Hosting.PubSub;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Keep this in the "Microsoft.Extensions.Configuration" for easy access.
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for registering message handlers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Discover the message handlers from the given assembly using the default <see cref="MessageHandlerAttribute"/> marker attribute.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <param name="handlerAssembly">The <see cref="Assembly"/> containing the handlers.</param>
    public static IServiceCollection AddPubSubMessageHandlers(this IServiceCollection services, Assembly handlerAssembly)
        => services
            .AddPubSubMessageHandlers<MessageHandlerAttribute>(handlerAssembly, a => a.Topic);

    /// <summary>
    /// Discover the message handlers from the given assembly.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register with.</param>
    /// <param name="handlerAssembly">The <see cref="Assembly"/> containing the handlers.</param>
    /// <param name="handlerTopicAccessor">The <see cref="Attribute"/> property that specifies the topic.</param>
    public static IServiceCollection AddPubSubMessageHandlers<TAttribute>(this IServiceCollection services,
        Assembly handlerAssembly, Func<TAttribute, string> handlerTopicAccessor)
        where TAttribute : Attribute
    {
        var handlerDefinitions = GetHandlerDefinitions(handlerAssembly, handlerTopicAccessor);
        foreach (var handlerDefinition in handlerDefinitions)
        {
            services.AddScoped(handlerDefinition.HandlerMethod.DeclaringType!);
        }

        // See https://github.com/dotnet/runtime/issues/38751
        // We no longer use AddHostedService(svc => new MessageHandlerBackgroundService(svc, handlerDefinitions)) due to that issue.
        return services.AddSingleton<IHostedService>(svc => new MessageHandlerBackgroundService(svc, handlerDefinitions));
    }

    private static IReadOnlyCollection<HandlerDefinitions> GetHandlerDefinitions<TAttribute>(Assembly assembly, Func<TAttribute, string> topicAccessor)
        where TAttribute : Attribute
    {
        var validator = new MethodInfoValidator();
        var result = new List<HandlerDefinitions>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var methodInfo in GetPublicMethods(assembly))
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var methodAttribute in methodInfo.GetCustomAttributes<TAttribute>())
            {
                var topic = topicAccessor(methodAttribute);
                if (!String.IsNullOrEmpty(topic) && validator.IsValidMethod(methodInfo))
                {
                    result.Add(new HandlerDefinitions(topic, methodInfo));
                }
            }
        }

        return result;
    }

    private static IEnumerable<MethodInfo> GetPublicMethods(Assembly assembly)
        => assembly.GetExportedTypes()
            .Where(w => w is { IsClass: true, IsAbstract: false })
            .SelectMany(s => s.GetMethods(BindingFlags.Public | BindingFlags.Instance));

    private sealed class MethodInfoValidator
    {
        private readonly Type _handlerReturnType = typeof(Task);
        private readonly Type _messageContractType = typeof(IMessage<>);
        private readonly Type _cancellationTokenType = typeof(CancellationToken);

        public bool IsValidMethod(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType != _handlerReturnType)
                return false;

            var methodParameters = methodInfo.GetParameters();
            if (methodParameters.Length != 2)
                return false;

            if (!methodParameters[0].ParameterType.IsGenericType)
                return false;

            if (methodParameters[0].ParameterType.GetGenericTypeDefinition() != _messageContractType)
                return false;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (methodParameters[1].ParameterType != _cancellationTokenType)
                return false;

            return true;
        }
    }
}
