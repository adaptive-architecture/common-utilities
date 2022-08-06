namespace AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

internal class HandlerRegistration
{
    public HandlerRegistration(string id, string topic, Delegate handler, Type handledType)
    {
        Id = id;
        Topic = topic;
        Handler = handler;
        HandledType = handledType;
    }

    public string Id { get; }
    public string Topic { get; }
    public Delegate Handler { get; }
    public Type HandledType { get; }
}
