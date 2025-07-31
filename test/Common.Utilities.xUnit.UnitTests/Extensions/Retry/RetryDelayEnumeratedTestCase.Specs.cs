using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using NSubstitute;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class RetryDelayEnumeratedTestCaseSpecs
{

    [Fact]
    public void Constructor_ShouldSetMaxRetries()
    {
        var testMethod = HelperTests.GetXunitTestMethod();
        var testCase = new RetryDelayEnumeratedTestCase(
            maxRetries: 5,
            testMethod: testMethod,
            testCaseDisplayName: "Test",
            uniqueID: "unique",
            @explicit: false,
            skipTestWithoutData: false
        );

        Assert.Equal(5, testCase.MaxRetries);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetPropertiesCorrectly()
    {
        var testMethod = HelperTests.GetXunitTestMethod();
        var skipExceptions = new[] { typeof(ArgumentException) };
        var traits = new Dictionary<string, HashSet<string>> { { "Category", new HashSet<string> { "Unit" } } };

        var testCase = new RetryDelayEnumeratedTestCase(
            maxRetries: 7,
            testMethod: testMethod,
            testCaseDisplayName: "DisplayName",
            uniqueID: "uniqueId",
            @explicit: true,
            skipTestWithoutData: true,
            skipExceptions: skipExceptions,
            skipReason: "SkipReason",
            skipType: typeof(string),
            skipUnless: "SkipUnless",
            skipWhen: "SkipWhen",
            traits: traits,
            sourceFilePath: "source.cs",
            sourceLineNumber: 123,
            timeout: 5000
        );

        Assert.Equal(7, testCase.MaxRetries);
        Assert.Equal("DisplayName", testCase.TestCaseDisplayName);
        Assert.Equal("uniqueId", testCase.UniqueID);
        Assert.True(testCase.Explicit);
        Assert.Equal("SkipReason", testCase.SkipReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(-1)]
    public void Constructor_WithVariousMaxRetries_ShouldSetCorrectly(int maxRetries)
    {
        var testMethod = HelperTests.GetXunitTestMethod();
        var testCase = new RetryDelayEnumeratedTestCase(
            maxRetries: maxRetries,
            testMethod: testMethod,
            testCaseDisplayName: "Test",
            uniqueID: "unique",
            @explicit: false,
            skipTestWithoutData: false
        );

        Assert.Equal(maxRetries, testCase.MaxRetries);
    }

    [Fact]
    public void Run_ShouldReturnSimilarResultAsRetryTestCaseRunner()
    {
        var messageBus = Substitute.For<IMessageBus>();
        var aggregator = new ExceptionAggregator();

        var testMethod = HelperTests.GetXunitTestMethod();
        var testCase = new RetryDelayEnumeratedTestCase(
            maxRetries: 5,
            testMethod: testMethod,
            testCaseDisplayName: "Test",
            uniqueID: "unique",
            @explicit: false,
            skipTestWithoutData: false
        );

        var expected = RetryTestCaseRunner.Instance.Run(
            maxRetries: 5,
            testCase: testCase,
            messageBus: messageBus,
            aggregator: aggregator,
            cancellationTokenSource: new CancellationTokenSource(),
            displayName: "Test",
            skipReason: null,
            explicitOption: ExplicitOption.On,
            constructorArguments: []
        );

        var actual = testCase.Run(
            explicitOption: ExplicitOption.On,
            messageBus: messageBus,
            constructorArguments: [],
            aggregator: aggregator,
            cancellationTokenSource: new CancellationTokenSource()
        );

        Assert.Equal(expected.ToString(), actual.ToString());
    }

    [Fact]
    public void Serialize_ShouldIncludeMaxRetries()
    {
        var testCase = new RetryDelayEnumeratedTestCase(
            maxRetries: 5,
            testMethod: HelperTests.GetXunitTestMethod(),
            testCaseDisplayName: "Test",
            uniqueID: "unique",
            @explicit: false,
            skipTestWithoutData: false
        );

        var serializationInfo = new XunitSerializationInfo(SerializationHelper.Instance);
        ((IXunitSerializable)testCase).Serialize(serializationInfo);

#pragma warning disable CS0618 // Type or member is obsolete
        var deserializedTestCase = new RetryDelayEnumeratedTestCase();
#pragma warning restore CS0618 // Type or member is obsolete
        ((IXunitSerializable)deserializedTestCase).Deserialize(serializationInfo);

        Assert.Equal(testCase.MaxRetries, deserializedTestCase.MaxRetries);
    }
}
