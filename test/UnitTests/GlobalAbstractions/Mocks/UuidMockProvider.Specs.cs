using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

namespace AdaptArch.UnitTests.GlobalAbstractions.Mocks;

public class UuidMockProviderSpecs
{
    [Fact]
    public void Should_Cycle_Through_The_Values()
    {
        var uuid0 = Guid.NewGuid();
        var uuid1 = Guid.NewGuid();
        var mockProvider = new UuidMockProvider(new[] {uuid0, uuid1});

        Assert.Equal(uuid0.ToString("D"), mockProvider.New());
        Assert.Equal(uuid1.ToString("D"), mockProvider.New());
        Assert.Equal(uuid0.ToString("D"), mockProvider.New());
    }
}
