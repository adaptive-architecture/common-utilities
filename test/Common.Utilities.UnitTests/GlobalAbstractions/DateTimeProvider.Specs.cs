using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.GlobalAbstractions;

public class DateTimeProviderSpecs
{
    [Fact]
    public void Should_Be_Same_Value_As_DateTime_UtcNow()
    {
        var start = DateTime.UtcNow;
        var providerDate = new DateTimeProvider().UtcNow;
        var end = DateTime.UtcNow;
        Assert.True(providerDate >= start);
        Assert.True(providerDate <= end);
    }
}
