/*
   Based on https://github.com/xunit/samples.xunit/blob/28d3683f74b104d33544efe5d1ae45ce9b0ad8c5/v3/RetryFactExample/
*/
using System.ComponentModel;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

// This class is used for facts, and for serializable pre-enumerated individual data rows in theories.
/// <summary>
/// Represents a test case that can be retried a specified number of times.
/// </summary>
public class RetryTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
#pragma warning disable S1133
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryTestCase"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public RetryTestCase() { }
#pragma warning restore S1133

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryTestCase"/> class with the specified parameters.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries for the test case.</param>
    /// <param name="testMethod">The test method to be executed.</param>
    /// <param name="testCaseDisplayName">The display name of the test case.</param>
    /// <param name="uniqueID">The unique identifier for the test case.</param>
    /// <param name="explicit">True if the test case is explicit; otherwise, false.</param>
    /// <param name="skipExceptions">The exceptions that should cause the test case to be skipped.</param>
    /// <param name="skipReason">The reason for skipping the test case.</param>
    /// <param name="skipType">The type of the skip condition.</param>
    /// <param name="skipUnless">The condition under which the test case should be skipped.</param>
    /// <param name="skipWhen">The condition under which the test case should be skipped.</param>
    /// <param name="traits">The traits associated with the test case.</param>
    /// <param name="testMethodArguments">The arguments for the test method.</param>
    /// <param name="sourceFilePath">The source file path where the test case is defined.</param>
    /// <param name="sourceLineNumber">The line number in the source file where the test case is defined.</param>
    /// <param name="timeout">The timeout for the test case execution.</param>
    public RetryTestCase(
        int maxRetries,
        IXunitTestMethod testMethod,
        string testCaseDisplayName,
        string uniqueID,
        bool @explicit,
        Type[]? skipExceptions = null,
        string? skipReason = null,
        Type? skipType = null,
        string? skipUnless = null,
        string? skipWhen = null,
        Dictionary<string, HashSet<string>>? traits = null,
        object?[]? testMethodArguments = null,
        string? sourceFilePath = null,
        int? sourceLineNumber = null,
        int? timeout = null) :
            base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
    {
        MaxRetries = maxRetries;
    }

    /// <inheritdoc/>
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
