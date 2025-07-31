/*
   Based on https://github.com/xunit/samples.xunit/blob/28d3683f74b104d33544efe5d1ae45ce9b0ad8c5/v3/RetryFactExample/
*/
using System.ComponentModel;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

/// <summary>
/// This class is used when pre-enumeration is disabled, or when the theory data was not serializable.
/// </summary>
public class RetryDelayEnumeratedTestCase : XunitDelayEnumeratedTheoryTestCase, ISelfExecutingXunitTestCase
{
#pragma warning disable S1133
    /// <summary>
    /// Constructor used by the de-serializer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public RetryDelayEnumeratedTestCase()
    { }
#pragma warning restore S1133

    /// <summary>
    /// Creates a new instance of <see cref="RetryDelayEnumeratedTestCase"/>.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries for the test case.</param>
    /// <param name="testMethod">The test method to execute.</param>
    /// <param name="testCaseDisplayName">The display name for the test case.</param>
    /// <param name="uniqueID">The unique identifier for the test case.</param>
    /// <param name="explicit">The explicit flag for the test case.</param>
    /// <param name="skipTestWithoutData">The flag indicating whether to skip the test if no data is available.</param>
    /// <param name="skipExceptions">The types of exceptions that should cause the test to be skipped.</param>
    /// <param name="skipReason">The reason for skipping the test case.</param>
    /// <param name="skipType">The type that determines whether the test should be skipped.</param>
    /// <param name="skipUnless">The condition that must be met to skip the test.</param>
    /// <param name="skipWhen">The condition that must be met to skip the test.</param>
    /// <param name="traits">The traits associated with the test case.</param>
    /// <param name="sourceFilePath">The source file path for the test case.</param>
    /// <param name="sourceLineNumber">The source line number for the test case.</param>
    /// <param name="timeout">The timeout for the test case.</param>
    public RetryDelayEnumeratedTestCase(
        int maxRetries,
        IXunitTestMethod testMethod,
        string testCaseDisplayName,
        string uniqueID,
        bool @explicit,
        bool skipTestWithoutData,
        Type[]? skipExceptions = null,
        string? skipReason = null,
        Type? skipType = null,
        string? skipUnless = null,
        string? skipWhen = null,
        Dictionary<string, HashSet<string>>? traits = null,
        string? sourceFilePath = null,
        int? sourceLineNumber = null,
        int? timeout = null) :
            base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipTestWithoutData, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, sourceFilePath, sourceLineNumber, timeout)
    {
        MaxRetries = maxRetries;
    }

    /// <summary>
    /// Gets or sets the maximum number of retries for the test case.
    /// </summary>
    public int MaxRetries { get; private set; }

    /// <inheritdoc/>
    protected override void Deserialize(IXunitSerializationInfo info)
    {
        base.Deserialize(info);

        MaxRetries = info.GetValue<int>(nameof(MaxRetries));
    }

    /// <inheritdoc/>

    public ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource) =>
            RetryTestCaseRunner.Instance.Run(
                MaxRetries,
                this,
                messageBus,
                aggregator.Clone(),
                cancellationTokenSource,
                TestCaseDisplayName,
                SkipReason,
                explicitOption,
                constructorArguments
            );

    /// <inheritdoc/>
    protected override void Serialize(IXunitSerializationInfo info)
    {
        base.Serialize(info);

        info.AddValue(nameof(MaxRetries), MaxRetries);
    }
}
