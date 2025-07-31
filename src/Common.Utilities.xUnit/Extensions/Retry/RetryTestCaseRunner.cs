/*
   Based on https://github.com/xunit/samples.xunit/blob/28d3683f74b104d33544efe5d1ae45ce9b0ad8c5/v3/RetryFactExample/
*/
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

/// <summary>
/// Works just like [Fact] except that failures are retried (by default, 3 times).
/// </summary>
public class RetryTestCaseRunner :
    XunitTestCaseRunnerBase<RetryTestCaseRunnerContext, IXunitTestCase, IXunitTest>
{
    /// <summary>
    /// Singleton instance of the <see cref="RetryTestCaseRunner"/>. This allows
    /// for a single instance to be reused across multiple test runs, which can
    /// help reduce memory usage and improve performance in scenarios where many
    /// retry test cases are executed.
    /// </summary>
    public static RetryTestCaseRunner Instance { get; } = new();

#pragma warning disable S107, S2325
    /// <inheritdoc/>
    public async ValueTask<RunSummary> Run(
        int maxRetries,
        IXunitTestCase testCase,
        IMessageBus messageBus,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        string displayName,
        string? skipReason,
        ExplicitOption explicitOption,
        object?[] constructorArguments)
    {
#pragma warning restore S107, S2325
        // This code comes from XunitRunnerHelper.RunXunitTestCase, and it's centralized
        // here just so we don't have to duplicate it in both RetryTestCase and
        // RetryDelayEnumeratedTestCase.
        var tests = await aggregator.RunAsync(testCase.CreateTests, []);

        if (aggregator.ToException() is Exception ex)
        {
            if (ex.Message.StartsWith(DynamicSkipToken.Value, StringComparison.Ordinal))
            {
                return XunitRunnerHelper.SkipTestCases(
                                messageBus,
                                cancellationTokenSource,
                                [testCase],
                                ex.Message[DynamicSkipToken.Value.Length..],
                                sendTestCaseMessages: false
                            );
            }
            else
            {
                return XunitRunnerHelper.FailTestCases(
                                messageBus,
                                cancellationTokenSource,
                                [testCase],
                                ex,
                                sendTestCaseMessages: false
                            );
            }
        }

        await using var ctx = new RetryTestCaseRunnerContext(maxRetries, testCase, tests, messageBus, aggregator, cancellationTokenSource, displayName, skipReason, explicitOption, constructorArguments);
        await ctx.InitializeAsync();

        return await Run(ctx);
    }

    /// <inheritdoc/>
    protected override async ValueTask<RunSummary> RunTest(
        RetryTestCaseRunnerContext ctx,
        IXunitTest test)
    {
        var runCount = 0;
        var maxRetries = ctx.MaxRetries;

        if (maxRetries < 1)
            maxRetries = 3;

        while (true)
        {
            // This is really the only tricky bit: we need to capture and delay messages (since those will
            // contain run status) until we know we've decided to accept the final result.
            var delayedMessageBus = new DelayedMessageBus(ctx.MessageBus);
            var aggregator = ctx.Aggregator.Clone();
            var result = await XunitTestRunner.Instance.Run(
                test,
                delayedMessageBus,
                ctx.ConstructorArguments,
                ctx.ExplicitOption,
                aggregator,
                ctx.CancellationTokenSource,
                ctx.BeforeAfterTestAttributes
            );

            if (!(aggregator.HasExceptions || result.Failed != 0) || ++runCount >= maxRetries)
            {
                delayedMessageBus.Dispose();  // Sends all the delayed messages
                return result;
            }

            TestContext.Current.SendDiagnosticMessage("Execution of '{0}' failed (attempt #{1}), retrying...", test.TestDisplayName, runCount);
            ctx.Aggregator.Clear();
        }
    }
}

/// <summary>
/// Context for running a retry test case.
/// </summary>
/// <param name="maxRetries">The maximum number of retries for the test case.</param>
/// <param name="testCase">The test case to be executed.</param>
/// <param name="tests">The tests associated with the test case.</param>
/// <param name="messageBus">The message bus to send messages to.</param>
/// <param name="aggregator">The exception aggregator to collect exceptions.</param>
/// <param name="cancellationTokenSource">The cancellation token source for the test run.</param>
/// <param name="displayName">The display name of the test case.</param>
/// <param name="skipReason">The reason for skipping the test case.</param>
/// <param name="explicitOption">The explicit option for the test case.</param>
/// <param name="constructorArguments">The constructor arguments for the test case.</param>
public class RetryTestCaseRunnerContext(
    int maxRetries,
    IXunitTestCase testCase,
    IReadOnlyCollection<IXunitTest> tests,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    string displayName,
    string? skipReason,
    ExplicitOption explicitOption,
    object?[] constructorArguments) :
        XunitTestCaseRunnerBaseContext<IXunitTestCase, IXunitTest>(testCase, tests, messageBus, aggregator, cancellationTokenSource, displayName, skipReason, explicitOption, constructorArguments)
{
    /// <summary>
    /// Gets the maximum number of retries for the test case.
    /// </summary>
    public int MaxRetries { get; } = maxRetries;
}
