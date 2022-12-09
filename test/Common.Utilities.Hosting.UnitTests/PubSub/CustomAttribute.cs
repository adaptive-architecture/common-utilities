namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class CustomAttribute : Attribute
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="topic">The name of the topic to handle.</param>
    public CustomAttribute(string topic) => Topic = topic;

    /// <summary>
    /// The topic this method can handle.
    /// </summary>
    public string Topic { get; init; }
}
