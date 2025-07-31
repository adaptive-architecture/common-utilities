using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using NSubstitute;
using Xunit;
using Xunit.Runner.Common;
using Xunit.Sdk;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

/// <summary>
/// Transparent wrapper around TheoryDiscoverer to access protected methods for testing
/// </summary>
public class TransparentTheoryDiscoverer : TheoryDiscoverer
{
    public ValueTask<IReadOnlyCollection<IXunitTestCase>> CallCreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object[] testMethodArguments)
    {
        return CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments);
    }

    public ValueTask<IReadOnlyCollection<IXunitTestCase>> CallCreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
    {
        return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
    }
}

/// <summary>
/// Transparent wrapper around RetryTheoryDiscoverer to access protected methods for testing
/// </summary>
public class TransparentRetryTheoryDiscoverer : RetryTheoryDiscoverer
{
    public ValueTask<IReadOnlyCollection<IXunitTestCase>> CallCreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object[] testMethodArguments)
    {
        return CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments);
    }

    public ValueTask<IReadOnlyCollection<IXunitTestCase>> CallCreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
    {
        return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
    }
}

public class RetryTheoryDiscovererSpecs
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        var discoverer = new RetryTheoryDiscoverer();

        Assert.NotNull(discoverer);
        Assert.IsType<RetryTheoryDiscoverer>(discoverer);
    }

    [Fact]
    public async Task CreateTestCasesForDataRow_WithRetryTheoryAttribute_ShouldReturnRetryTestCase()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute { MaxRetries = 5 };
        var dataRow = Substitute.For<ITheoryDataRow>();
        var testMethodArguments = new object[] { "arg1", 42 };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForDataRow(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute,
            dataRow,
            testMethodArguments);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryTestCase>(result.First());
        Assert.Equal(5, testCase.MaxRetries);
        Assert.Equal(testMethodArguments, testCase.TestMethodArguments);
    }

    [Fact]
    public async Task CreateTestCasesForDataRow_WithDefaultRetryTheoryAttribute_ShouldUseDefaultMaxRetries()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute(); // Default MaxRetries = 3
        var dataRow = Substitute.For<ITheoryDataRow>();
        var testMethodArguments = new object[] { "test" };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForDataRow(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute,
            dataRow,
            testMethodArguments);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryTestCase>(result.First());
        Assert.Equal(3, testCase.MaxRetries);
    }

    [Fact]
    public async Task CreateTestCasesForDataRow_VsStandardTheoryDiscoverer_ShouldReturnDifferentTestCaseTypes()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var standardDiscoverer = new TransparentTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var retryTheoryAttribute = new RetryTheoryAttribute { MaxRetries = 7 };
        var standardTheoryAttribute = Substitute.For<ITheoryAttribute>();
        var dataRow = Substitute.For<ITheoryDataRow>();
        var testMethodArguments = new object[] { "compare" };

        // Act
        var retryResult = await retryDiscoverer.CallCreateTestCasesForDataRow(
            TestFrameworkOptions.Empty(),
            testMethod,
            retryTheoryAttribute,
            dataRow,
            testMethodArguments);

        var standardResult = await standardDiscoverer.CallCreateTestCasesForDataRow(
            TestFrameworkOptions.Empty(),
            testMethod,
            standardTheoryAttribute,
            dataRow,
            testMethodArguments);

        // Assert
        Assert.Single(retryResult);
        Assert.Single(standardResult);

        var retryTestCase = Assert.IsType<RetryTestCase>(retryResult.First());
        var standardTestCase = Assert.IsType<XunitTestCase>(standardResult.First());

        Assert.Equal(7, retryTestCase.MaxRetries);
        Assert.NotEqual(retryTestCase.GetType(), standardTestCase.GetType());
    }

    [Fact]
    public async Task CreateTestCasesForTheory_WithRetryTheoryAttribute_ShouldReturnRetryDelayEnumeratedTestCase()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute { MaxRetries = 4 };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryDelayEnumeratedTestCase>(result.First());
        Assert.Equal(4, testCase.MaxRetries);
    }

    [Fact]
    public async Task CreateTestCasesForTheory_WithSkipReason_ShouldReturnXunitTestCase()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute
        {
            MaxRetries = 6,
            Skip = "Test is skipped"
        };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<XunitTestCase>(result.First());
        Assert.Equal("Test is skipped", testCase.SkipReason);
    }

    [Fact]
    public async Task CreateTestCasesForTheory_WithDefaultRetryTheoryAttribute_ShouldUseDefaultMaxRetries()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute(); // Default MaxRetries = 3

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryDelayEnumeratedTestCase>(result.First());
        Assert.Equal(3, testCase.MaxRetries);
    }

    [Fact]
    public async Task CreateTestCasesForTheory_VsStandardTheoryDiscoverer_ShouldReturnDifferentTestCaseTypes()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var standardDiscoverer = new TransparentTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var retryTheoryAttribute = new RetryTheoryAttribute { MaxRetries = 8 };
        var standardTheoryAttribute = Substitute.For<ITheoryAttribute>();

        // Act
        var retryResult = await retryDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            retryTheoryAttribute);

        var standardResult = await standardDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            standardTheoryAttribute);

        // Assert
        Assert.Single(retryResult);
        Assert.Single(standardResult);

        var retryTestCase = Assert.IsType<RetryDelayEnumeratedTestCase>(retryResult.First());
        var standardTestCase = Assert.IsType<XunitDelayEnumeratedTheoryTestCase>(standardResult.First());

        Assert.Equal(8, retryTestCase.MaxRetries);
        Assert.NotEqual(retryTestCase.GetType(), standardTestCase.GetType());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task CreateTestCasesForDataRow_WithVariousMaxRetries_ShouldSetCorrectMaxRetries(int maxRetries)
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute { MaxRetries = maxRetries };
        var dataRow = Substitute.For<ITheoryDataRow>();
        var testMethodArguments = new object[] { "test-value" };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForDataRow(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute,
            dataRow,
            testMethodArguments);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryTestCase>(result.First());
        Assert.Equal(maxRetries, testCase.MaxRetries);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(50)]
    public async Task CreateTestCasesForTheory_WithVariousMaxRetries_ShouldSetCorrectMaxRetries(int maxRetries)
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var theoryAttribute = new RetryTheoryAttribute { MaxRetries = maxRetries };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            theoryAttribute);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryDelayEnumeratedTestCase>(result.First());
        Assert.Equal(maxRetries, testCase.MaxRetries);
    }

    [Fact]
    public async Task CreateTestCasesForDataRow_WithNonRetryTheoryAttribute_ShouldUseDefaultMaxRetries()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var nonRetryTheoryAttribute = Substitute.For<ITheoryAttribute>();
        var dataRow = Substitute.For<ITheoryDataRow>();
        var testMethodArguments = new object[] { "non-retry" };

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForDataRow(
            TestFrameworkOptions.Empty(),
            testMethod,
            nonRetryTheoryAttribute,
            dataRow,
            testMethodArguments);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryTestCase>(result.First());
        Assert.Equal(3, testCase.MaxRetries); // Default value
    }

    [Fact]
    public async Task CreateTestCasesForTheory_WithNonRetryTheoryAttribute_ShouldUseDefaultMaxRetries()
    {
        // Arrange
        var retryDiscoverer = new TransparentRetryTheoryDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var nonRetryTheoryAttribute = Substitute.For<ITheoryAttribute>();

        // Act
        var result = await retryDiscoverer.CallCreateTestCasesForTheory(
            TestFrameworkOptions.Empty(),
            testMethod,
            nonRetryTheoryAttribute);

        // Assert
        Assert.Single(result);
        var testCase = Assert.IsType<RetryDelayEnumeratedTestCase>(result.First());
        Assert.Equal(3, testCase.MaxRetries); // Default value
    }
}
