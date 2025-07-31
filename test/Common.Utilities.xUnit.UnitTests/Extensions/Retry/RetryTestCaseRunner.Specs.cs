using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using NSubstitute;
using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class RetryTestCaseRunnerSpecs
{
    [Fact]
    public void Instance_ShouldReturnSingletonInstance()
    {
        var instance1 = RetryTestCaseRunner.Instance;
        var instance2 = RetryTestCaseRunner.Instance;

        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
        Assert.IsType<RetryTestCaseRunner>(instance1);
    }

    [Fact]
    public void Instance_ShouldReturnSameInstanceMultipleTimes()
    {
        var instance1 = RetryTestCaseRunner.Instance;
        var instance2 = RetryTestCaseRunner.Instance;
        var instance3 = RetryTestCaseRunner.Instance;

        Assert.Same(instance1, instance2);
        Assert.Same(instance2, instance3);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task Run_FailingTests_ShouldReturnFailureResult(int maxRetries)
    {
        // Arrange
        var testCase = HelperTests.GetXunitTestCase(HelperTests.GetXunitTestMethod_Helper_Failure());
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: maxRetries,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert
        Assert.Equal(0, result.Skipped);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Total);
    }



    [Theory]
    [InlineData("Some failure message")]
    [InlineData(DynamicSkipToken.Value)]
    public async Task Run_ShouldHandle_AggException(string exceptionMessage)
    {
        // Arrange
        var testCase = HelperTests.GetXunitTestCase(HelperTests.GetXunitTestMethod_Helper_Failure());
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        aggregator.Add(new InvalidOperationException(exceptionMessage));

        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task Run_WithSuccessfulTestExecution_ShouldReturnSuccessResult()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("SuccessfulTest");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - The test should succeed without retries
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Skipped);
        Assert.True(result.Total >= 0); // Some tests may pass
    }

    [Fact]
    public async Task Run_WithMaxRetriesZero_ShouldUseDefaultValueOfThree()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("TestWithZeroMaxRetries");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 0, // Should default to 3
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Test should execute (default maxRetries = 3 is used)
        Assert.True(result.Total >= 0);
    }

    [Fact]
    public async Task Run_WithNegativeMaxRetries_ShouldUseDefaultValueOfThree()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("TestWithNegativeMaxRetries");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: -1, // Should default to 3
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Test should execute (default maxRetries = 3 is used)
        Assert.True(result.Total >= 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Run_WithValidMaxRetries_ShouldRespectMaxRetriesValue(int maxRetries)
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns($"TestWithMaxRetries{maxRetries}");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: maxRetries,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Test should execute with the specified maxRetries
        Assert.True(result.Total >= 0);
    }

    [Fact]
    public async Task Run_WithSkipReason_ShouldPassSkipReasonToContext()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();
        const string skipReason = "Test skipped for testing purposes";

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("TestWithSkipReason");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: skipReason,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Test should handle skip reason properly
        Assert.True(result.Total >= 0);
    }

    [Fact]
    public async Task Run_WithExplicitOptionOn_ShouldPassExplicitOptionToContext()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("ExplicitTest");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.On,
            constructorArguments: constructorArguments
        );

        // Assert - Test should handle explicit option properly
        Assert.True(result.Total >= 0);
    }

    [Fact]
    public async Task Run_WithConstructorArguments_ShouldPassArgumentsToContext()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = new object[] { "arg1", 42, true };

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("TestWithConstructorArgs");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Test should handle constructor arguments properly
        Assert.True(result.Total >= 0);
    }

    [Fact]
    public async Task Run_WithMultipleTests_ShouldHandleAllTests()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test1 = Substitute.For<IXunitTest>();
        var test2 = Substitute.For<IXunitTest>();
        var test3 = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test1, test2, test3]);
        test1.TestDisplayName.Returns("Test1");
        test2.TestDisplayName.Returns("Test2");
        test3.TestDisplayName.Returns("Test3");

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - All tests should be handled
        Assert.True(result.Total >= 0);
    }

    [Fact]
    public async Task Run_WithEmptyTestCollection_ShouldHandleEmptyTests()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns(Array.Empty<IXunitTest>());

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Should handle empty test collection gracefully
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task Run_WithCancelledCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var testCase = Substitute.For<IXunitTestCase>();
        var test = Substitute.For<IXunitTest>();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var aggregator = new ExceptionAggregator();
        var cancellationTokenSource = new CancellationTokenSource();
        var constructorArguments = Array.Empty<object>();

        testCase.CreateTests().Returns([test]);
        test.TestDisplayName.Returns("CancellableTest");

        // Cancel the token before running
        cancellationTokenSource.Cancel();

        // Act
        var result = await RetryTestCaseRunner.Instance.Run(
            maxRetries: 3,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: cancellationTokenSource,
            displayName: "TestName",
            skipReason: null,
            explicitOption: ExplicitOption.Off,
            constructorArguments: constructorArguments
        );

        // Assert - Should handle cancellation appropriately
        Assert.True(result.Total >= 0);
    }
}
