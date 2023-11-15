using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.GlobalAbstractions;

public class TimeProviderWrapperSpecs
{
    [Fact]
    public void Should_Be_Same_Value_As_DateTime_UtcNow()
    {
        var start = TimeProvider.System.GetUtcNow().UtcDateTime;
        var providerDate = new TimeProviderWrapper(TimeProvider.System).UtcNow;
        var end = TimeProvider.System.GetUtcNow().UtcDateTime;
        Assert.True(providerDate >= start);
        Assert.True(providerDate <= end);
    }

    [Fact]
    public void Should_Throw_If_Null_Argument_Passed_To_Constructor()
    {
        Assert.Throws<ArgumentNullException>(() => new TimeProviderWrapper(null!));
    }
}
