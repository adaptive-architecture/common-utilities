using System.Reflection;
using AdaptArch.Common.Utilities.Hosting.PubSub;
using AdaptArch.Common.Utilities.PubSub.Contracts;
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

        return services.AddSingleton<IHostedService>(svc => new MessageHandlerBackgroundService(svc, handlerDefinitions));
        // See https://github.com/dotnet/runtime/issues/38751
        //return services.AddHostedService(svc => new MessageHandlerBackgroundService(svc, handlerDefinitions));
    }

    private static IReadOnlyCollection<HandlerDefinitions> GetHandlerDefinitions<TAttribute>(Assembly assembly, Func<TAttribute, string> topicAccessor)
        where TAttribute : Attribute
    {
        var handlerReturnType = typeof(Task);
        var messageContractType = typeof(IMessage<>);
        var cancellationTokenType = typeof(CancellationToken);

        var result = new List<HandlerDefinitions>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var methodInfo in GetPublicMethods(assembly))
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var methodAttribute in methodInfo.GetCustomAttributes<TAttribute>())
            {
                var topic = topicAccessor(methodAttribute);
                if (String.IsNullOrEmpty(topic))
                    continue;

                if (methodInfo.ReturnType != handlerReturnType)
                    continue; // Return type does not match the needed type.

                var methodParameters = methodInfo.GetParameters();
                if (methodParameters.Length != 2)
                    continue; // We require 2 parameters.

                if (!methodParameters[0].ParameterType.IsGenericType)
                    continue; // The first parameter should be a IMessage<TClass>.

                if (methodParameters[0].ParameterType.GetGenericTypeDefinition() != messageContractType)
                    continue; // The first parameter should be a IMessage<TClass>.

                if (methodParameters[1].ParameterType != cancellationTokenType)
                    continue; // The seconds parameter should be a cancellation token.

                result.Add(new HandlerDefinitions(topic, methodInfo));
            }
        }

        return result;
    }

    private static IEnumerable<MethodInfo> GetPublicMethods(Assembly assembly)
        => assembly.GetExportedTypes()
            .Where(w => w.IsClass && !w.IsAbstract)
            .SelectMany(s => s.GetMethods(BindingFlags.Public | BindingFlags.Instance));
}
