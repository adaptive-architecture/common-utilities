namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// A marker attribute for message handler functions.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class MessageHandlerAttribute : Attribute
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="topic">The name of the topic to handle.</param>
    public MessageHandlerAttribute(string topic) => Topic = topic;

    /// <summary>
    /// The topic this method can handle.
    /// </summary>
    public string Topic { get; init; }
}
