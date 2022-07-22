using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.UnitTests.GlobalAbstractions;

public class UuidSpecs
{
    [Fact]
    public void Should_Be_A_Valid_Uuid_Without_Dashes()
    {
        var uuid = new UnDashedUuidProvider().New();
        Assert.False(uuid.Contains('-'));
        Assert.Equal(Guid.Parse(uuid).ToString("N"), uuid);
    }
    [Fact]
    public void Should_Be_A_Valid_Uuid_With_Dashes()
    {
        var uuid = new DashedUuidProvider().New();
        Assert.True(uuid.Contains('-'));
        Assert.Equal(Guid.Parse(uuid).ToString("D"), uuid);
    }
}
