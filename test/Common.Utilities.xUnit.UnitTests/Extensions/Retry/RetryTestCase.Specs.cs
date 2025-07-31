using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class RetryTestCaseSpecs
{
    private static RetryTestCase CreateTestCase(int maxRetries = 5)
    {
        return new RetryTestCase(
            maxRetries: maxRetries,
            testMethod: HelperTests.GetXunitTestMethod(),
            testCaseDisplayName: "Test",
            uniqueID: "unique",
            @explicit: false
        );
    }

    [Fact]
    public void Serialize_ShouldIncludeMaxRetries()
    {
        var testCase = CreateTestCase();

        var serializationInfo = new XunitSerializationInfo(SerializationHelper.Instance);
        ((IXunitSerializable)testCase).Serialize(serializationInfo);

#pragma warning disable CS0618 // Type or member is obsolete
        var deserializedTestCase = new RetryTestCase();
#pragma warning restore CS0618 // Type or member is obsolete
        ((IXunitSerializable)deserializedTestCase).Deserialize(serializationInfo);

        Assert.Equal(testCase.MaxRetries, deserializedTestCase.MaxRetries);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetPropertiesCorrectly()
    {
        var testMethod = HelperTests.GetXunitTestMethod();
        var skipExceptions = new[] { typeof(ArgumentException) };
        var traits = new Dictionary<string, HashSet<string>> { { "Category", new HashSet<string> { "Unit" } } };
        var testMethodArguments = new object[] { "arg1", 42 };

        var testCase = new RetryTestCase(
            maxRetries: 7,
            testMethod: testMethod,
            testCaseDisplayName: "DisplayName",
            uniqueID: "uniqueId",
            @explicit: true,
            skipExceptions: skipExceptions,
            skipReason: "SkipReason",
            skipType: typeof(string),
            skipUnless: "SkipUnless",
            skipWhen: "SkipWhen",
            traits: traits,
            testMethodArguments: testMethodArguments,
            sourceFilePath: "source.cs",
            sourceLineNumber: 123,
            timeout: 5000
        );

        Assert.Equal(7, testCase.MaxRetries);
        Assert.Equal("DisplayName", testCase.TestCaseDisplayName);
        Assert.Equal("uniqueId", testCase.UniqueID);
        Assert.True(testCase.Explicit);
        Assert.Equal("SkipReason", testCase.SkipReason);
        Assert.Equal(testMethodArguments, testCase.TestMethodArguments);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(-1)]
    public void Constructor_WithVariousMaxRetries_ShouldSetCorrectly(int maxRetries)
    {
        var testCase = CreateTestCase(maxRetries);

        Assert.Equal(maxRetries, testCase.MaxRetries);
    }

    [Fact]
    public async ValueTask Run_ShouldCallRetryTestCaseRunner()
    {
        var testCase = CreateTestCase();
        var aggregator = new ExceptionAggregator();
        var messageBus = new MessageBus(NullMessageSink.Instance, false);
        var expected = await RetryTestCaseRunner.Instance.Run(testCase.MaxRetries, testCase, messageBus, aggregator, new CancellationTokenSource(), nameof(Run_ShouldCallRetryTestCaseRunner), null, ExplicitOption.Off, []);
        var actual = await testCase.Run(ExplicitOption.Off, messageBus, [], aggregator, new CancellationTokenSource());

        Assert.Equal(expected.Total, actual.Total);
    }
}
