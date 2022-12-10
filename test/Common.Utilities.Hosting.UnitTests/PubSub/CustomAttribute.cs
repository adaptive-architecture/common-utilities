namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class CustomAttribute : Attribute
{
    private readonly KnownTopics _knownTopic;
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="topic">The name of the topic to handle.</param>
    public CustomAttribute(KnownTopics topic) => _knownTopic = topic;

    /// <summary>
    /// The topic this method can handle.
    /// </summary>
    public string Topic => _knownTopic.ToString("G");
}

public enum KnownTopics
{
    None,
    MyCustomTopic
}
