using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using NSubstitute;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class RetryTestCaseRunnerContextSpecs
{
    [Fact]
    public void Constructor_ShouldSetMaxRetries()
    {
        var testCase = HelperTests.GetXunitTestCase();
        var tests = new List<IXunitTest>();
        var messageBus = Substitute.For<IMessageBus>();
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        var context = new RetryTestCaseRunnerContext(
            maxRetries: 5,
            testCase: testCase,
            tests: tests,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestDisplayName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        Assert.Equal(5, context.MaxRetries);
    }

    [Fact]
    public void Constructor_ShouldInitializeBaseProperties()
    {
        var testCase = HelperTests.GetXunitTestCase();
        var tests = new List<IXunitTest>();
        var messageBus = Substitute.For<IMessageBus>();
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = new object[] { "arg1", 42 };

        var context = new RetryTestCaseRunnerContext(
            maxRetries: 3,
            testCase: testCase,
            tests: tests,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestDisplayName",
            skipReason: "SkipReason",
            explicitOption: ExplicitOption.On,
            constructorArguments: constructorArguments
        );

        Assert.Same(testCase, context.TestCase);
        Assert.Same(tests, context.Tests);
        Assert.Same(messageBus, context.MessageBus);
        Assert.Equal(aggregator, context.Aggregator);
        Assert.Same(cancellationTokenSource, context.CancellationTokenSource);
        Assert.Equal("TestDisplayName", context.DisplayName);
        Assert.Equal("SkipReason", context.SkipReason);
        Assert.Equal(ExplicitOption.On, context.ExplicitOption);
        Assert.Equal(constructorArguments, context.ConstructorArguments);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(-1)]
    public void Constructor_WithVariousMaxRetries_ShouldSetCorrectly(int maxRetries)
    {
        var testCase = HelperTests.GetXunitTestCase();
        var tests = new List<IXunitTest>();
        var messageBus = Substitute.For<IMessageBus>();
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        var context = new RetryTestCaseRunnerContext(
            maxRetries: maxRetries,
            testCase: testCase,
            tests: tests,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "Test",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        Assert.Equal(maxRetries, context.MaxRetries);
    }
}
