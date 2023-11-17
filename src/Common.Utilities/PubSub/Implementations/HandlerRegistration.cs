namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// A class to keep track of the handlers registered in the application.
/// </summary>
public sealed class HandlerRegistration
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id">The registration UUID.</param>
    /// <param name="topic">The topic.</param>
    /// <param name="handler">The handler.</param>
    /// <param name="handledType">THe handler type.</param>
    public HandlerRegistration(string id, string topic, Delegate handler, Type handledType)
    {
        Id = id;
        Topic = topic;
        Handler = handler;
        HandledType = handledType;
    }

    /// <summary>
    /// A UUID to track the registration.
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// The topic.
    /// </summary>
    public string Topic { get; }
    /// <summary>
    /// THe handler.
    /// </summary>
    public Delegate Handler { get; }
    /// <summary>
    /// The handler type.
    /// </summary>
    public Type HandledType { get; }
}
