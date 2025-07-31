using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using Xunit;
using Xunit.Runner.Common;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class RetryFactDiscovererSpecs
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        var discoverer = new RetryFactDiscoverer();

        Assert.NotNull(discoverer);
        Assert.IsType<RetryFactDiscoverer>(discoverer);
    }

    [Fact]
    public async Task Discover_ShouldReturnRetryTestCase()
    {
        var discoverer = new RetryFactDiscoverer();
        var testMethod = HelperTests.GetXunitTestMethod();
        var factAttribute = new RetryFactAttribute { MaxRetries = 5 };

        var result = await discoverer.Discover(TestFrameworkOptions.Empty(), testMethod, factAttribute);

        Assert.Single(result);
        var testCase = Assert.IsType<RetryTestCase>(result.First());
        Assert.Equal(5, testCase.MaxRetries);
    }
}
