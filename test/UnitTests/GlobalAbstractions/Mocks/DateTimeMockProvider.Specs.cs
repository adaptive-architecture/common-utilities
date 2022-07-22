using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

namespace AdaptArch.UnitTests.GlobalAbstractions.Mocks;

public class DateTimeProviderSpecs
{
    [Fact]
    public void Should_Cycle_Through_The_Values()
    {
        var now = DateTime.UtcNow;
        var mockProvider = new DateTimeMockProvider(new[] {DateTime.UnixEpoch, now});

        Assert.Equal(DateTime.UnixEpoch, mockProvider.UtcNow);
        Assert.Equal(now, mockProvider.UtcNow);
        Assert.Equal(DateTime.UnixEpoch, mockProvider.UtcNow);
    }
}
