namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// Configuration options for <see cref="InProcessMessageHub"/>.
/// </summary>
public class InProcessMessageHubOptions : MessageHubOptions
{
    /// <summary>
    /// The hub's maximum degree of parallelism.
    /// This controls:
    ///  - How many handlers should be called in parallel as the result of a "publish".
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
