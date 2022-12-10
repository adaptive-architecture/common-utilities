using System.Reflection;

namespace AdaptArch.Common.Utilities.Hosting.PubSub;

internal class HandlerDefinitions
{
    internal HandlerDefinitions(string topic, MethodInfo handlerMethod)
    {
        Topic = topic;
        HandlerMethod = handlerMethod;
    }

    internal string Topic { get; init; }

    internal MethodInfo HandlerMethod { get; init; }
}
