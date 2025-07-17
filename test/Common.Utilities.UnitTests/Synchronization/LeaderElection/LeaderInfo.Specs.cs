using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class LeaderInfoSpecs
{
    private readonly DateTime _baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string DefaultParticipantId = "participant-1";
    private const string AlternateParticipantId = "participant-2";

    [Fact]
    public void Constructor_WithRequiredProperties_ShouldInitializeCorrectly()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);

        // Act
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt
        };

        // Assert
        Assert.Equal(DefaultParticipantId, leaderInfo.ParticipantId);
        Assert.Equal(acquiredAt, leaderInfo.AcquiredAt);
        Assert.Equal(expiresAt, leaderInfo.ExpiresAt);
        Assert.Null(leaderInfo.Metadata);
    }

    [Fact]
    public void Constructor_WithAllProperties_ShouldInitializeCorrectly()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(DefaultParticipantId, leaderInfo.ParticipantId);
        Assert.Equal(acquiredAt, leaderInfo.AcquiredAt);
        Assert.Equal(expiresAt, leaderInfo.ExpiresAt);
        Assert.Equal(metadata, leaderInfo.Metadata);
    }

    [Fact]
    public void Constructor_WithEmptyMetadata_ShouldInitializeCorrectly()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string>();

        // Act
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(DefaultParticipantId, leaderInfo.ParticipantId);
        Assert.Equal(acquiredAt, leaderInfo.AcquiredAt);
        Assert.Equal(expiresAt, leaderInfo.ExpiresAt);
        Assert.Equal(metadata, leaderInfo.Metadata);
        Assert.Empty(leaderInfo.Metadata);
    }

    [Fact]
    public void IsValid_WhenNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddMinutes(5);
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = futureTime
        };

        // Act & Assert
        Assert.True(leaderInfo.IsValid);
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddMinutes(-5);
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = pastTime
        };

        // Act & Assert
        Assert.False(leaderInfo.IsValid);
    }

    [Fact]
    public void IsValid_WhenExpiresAtCurrentTime_ShouldReturnFalse()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = currentTime
        };

        // Act & Assert
        Assert.False(leaderInfo.IsValid);
    }

    [Fact]
    public void TimeToExpiry_WhenNotExpired_ShouldReturnPositiveTimeSpan()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddMinutes(5);
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = futureTime
        };

        // Act
        var timeToExpiry = leaderInfo.TimeToExpiry;

        // Assert
        Assert.True(timeToExpiry > TimeSpan.Zero);
        Assert.True(timeToExpiry <= TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void TimeToExpiry_WhenExpired_ShouldReturnNegativeTimeSpan()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddMinutes(-5);
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = pastTime
        };

        // Act
        var timeToExpiry = leaderInfo.TimeToExpiry;

        // Assert
        Assert.True(timeToExpiry < TimeSpan.Zero);
    }

    [Fact]
    public void TimeToExpiry_WhenExpiresAtCurrentTime_ShouldReturnApproximatelyZero()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = currentTime
        };

        // Act
        var timeToExpiry = leaderInfo.TimeToExpiry;

        // Assert
        Assert.True(Math.Abs(timeToExpiry.TotalMilliseconds) < 1000); // Within 1 second
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Act & Assert
        Assert.Equal(leaderInfo1, leaderInfo2);
        Assert.True(leaderInfo1 == leaderInfo2);
        Assert.False(leaderInfo1 != leaderInfo2);
    }

    [Fact]
    public void Equality_WithDifferentParticipantId_ShouldNotBeEqual()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = AlternateParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt
        };

        // Act & Assert
        Assert.NotEqual(leaderInfo1, leaderInfo2);
        Assert.False(leaderInfo1 == leaderInfo2);
        Assert.True(leaderInfo1 != leaderInfo2);
    }

    [Fact]
    public void Equality_WithDifferentAcquiredAt_ShouldNotBeEqual()
    {
        // Arrange
        var acquiredAt1 = _baseTime;
        var acquiredAt2 = _baseTime.AddMinutes(1);
        var expiresAt = _baseTime.AddMinutes(5);

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt1,
            ExpiresAt = expiresAt
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt2,
            ExpiresAt = expiresAt
        };

        // Act & Assert
        Assert.NotEqual(leaderInfo1, leaderInfo2);
        Assert.False(leaderInfo1 == leaderInfo2);
        Assert.True(leaderInfo1 != leaderInfo2);
    }

    [Fact]
    public void Equality_WithDifferentExpiresAt_ShouldNotBeEqual()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt1 = _baseTime.AddMinutes(5);
        var expiresAt2 = _baseTime.AddMinutes(10);

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt1
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt2
        };

        // Act & Assert
        Assert.NotEqual(leaderInfo1, leaderInfo2);
        Assert.False(leaderInfo1 == leaderInfo2);
        Assert.True(leaderInfo1 != leaderInfo2);
    }

    [Fact]
    public void Equality_WithDifferentMetadata_ShouldNotBeEqual()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata1 = new Dictionary<string, string> { ["key"] = "value1" };
        var metadata2 = new Dictionary<string, string> { ["key"] = "value2" };

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata1
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata2
        };

        // Act & Assert
        Assert.NotEqual(leaderInfo1, leaderInfo2);
        Assert.False(leaderInfo1 == leaderInfo2);
        Assert.True(leaderInfo1 != leaderInfo2);
    }

    [Fact]
    public void Equality_WithNullAndEmptyMetadata_ShouldNotBeEqual()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var emptyMetadata = new Dictionary<string, string>();

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = null
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = emptyMetadata
        };

        // Act & Assert
        Assert.NotEqual(leaderInfo1, leaderInfo2);
        Assert.False(leaderInfo1 == leaderInfo2);
        Assert.True(leaderInfo1 != leaderInfo2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Act & Assert
        Assert.Equal(leaderInfo1.GetHashCode(), leaderInfo2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = AlternateParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt
        };

        // Act & Assert
        Assert.NotEqual(leaderInfo1.GetHashCode(), leaderInfo2.GetHashCode());
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithModifiedValues()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var originalMetadata = new Dictionary<string, string> { ["key"] = "value" };

        var originalLeaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = originalMetadata
        };

        // Act
        var modifiedLeaderInfo = originalLeaderInfo with { ParticipantId = AlternateParticipantId };

        // Assert
        Assert.NotEqual(originalLeaderInfo, modifiedLeaderInfo);
        Assert.Equal(AlternateParticipantId, modifiedLeaderInfo.ParticipantId);
        Assert.Equal(acquiredAt, modifiedLeaderInfo.AcquiredAt);
        Assert.Equal(expiresAt, modifiedLeaderInfo.ExpiresAt);
        Assert.Equal(originalMetadata, modifiedLeaderInfo.Metadata);
    }

    [Fact]
    public void WithExpression_ModifyingExpiresAt_ShouldUpdateTimeProperties()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var originalExpiresAt = _baseTime.AddMinutes(5);
        var newExpiresAt = DateTime.UtcNow.AddMinutes(10);

        var originalLeaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = originalExpiresAt
        };

        // Act
        var modifiedLeaderInfo = originalLeaderInfo with { ExpiresAt = newExpiresAt };

        // Assert
        Assert.NotEqual(originalLeaderInfo, modifiedLeaderInfo);
        Assert.Equal(newExpiresAt, modifiedLeaderInfo.ExpiresAt);
        Assert.True(modifiedLeaderInfo.IsValid);
        Assert.True(modifiedLeaderInfo.TimeToExpiry > TimeSpan.Zero);
    }

    [Fact]
    public void WithExpression_ModifyingMetadata_ShouldUpdateMetadata()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var originalMetadata = new Dictionary<string, string> { ["key"] = "value" };
        var newMetadata = new Dictionary<string, string> { ["newKey"] = "newValue" };

        var originalLeaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = originalMetadata
        };

        // Act
        var modifiedLeaderInfo = originalLeaderInfo with { Metadata = newMetadata };

        // Assert
        Assert.NotEqual(originalLeaderInfo, modifiedLeaderInfo);
        Assert.Equal(newMetadata, modifiedLeaderInfo.Metadata);
        Assert.NotEqual(originalMetadata, modifiedLeaderInfo.Metadata);
    }

    [Fact]
    public void WithExpression_SettingMetadataToNull_ShouldWork()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var originalMetadata = new Dictionary<string, string> { ["key"] = "value" };

        var originalLeaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = originalMetadata
        };

        // Act
        var modifiedLeaderInfo = originalLeaderInfo with { Metadata = null };

        // Assert
        Assert.NotEqual(originalLeaderInfo, modifiedLeaderInfo);
        Assert.Null(modifiedLeaderInfo.Metadata);
        Assert.NotNull(originalLeaderInfo.Metadata);
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulString()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Act
        var stringRepresentation = leaderInfo.ToString();

        // Assert
        Assert.Contains(DefaultParticipantId, stringRepresentation);
        Assert.Contains(nameof(LeaderInfo), stringRepresentation);
    }

    [Fact]
    public void ToString_WithNullMetadata_ShouldReturnMeaningfulString()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);

        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = null
        };

        // Act
        var stringRepresentation = leaderInfo.ToString();

        // Assert
        Assert.Contains(DefaultParticipantId, stringRepresentation);
        Assert.Contains(nameof(LeaderInfo), stringRepresentation);
    }

    [Fact]
    public void Record_ShouldBehaveLikeValueType()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        var leaderInfo1 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        var leaderInfo2 = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Act & Assert - Records should behave like value types
        Assert.Equal(leaderInfo1, leaderInfo2);
        Assert.True(leaderInfo1 == leaderInfo2);
        Assert.Equal(leaderInfo1.GetHashCode(), leaderInfo2.GetHashCode());
    }

    [Fact]
    public void PropertyAccessors_ShouldBeReadOnly()
    {
        // Arrange
        var acquiredAt = _baseTime;
        var expiresAt = _baseTime.AddMinutes(5);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = acquiredAt,
            ExpiresAt = expiresAt,
            Metadata = metadata
        };

        // Act & Assert - Properties should be accessible but not settable after construction
        Assert.Equal(DefaultParticipantId, leaderInfo.ParticipantId);
        Assert.Equal(acquiredAt, leaderInfo.AcquiredAt);
        Assert.Equal(expiresAt, leaderInfo.ExpiresAt);
        Assert.Equal(metadata, leaderInfo.Metadata);

        // Verify that properties are init-only (this is compile-time enforced)
        // The compiler would prevent: leaderInfo.ParticipantId = "new-value";
    }

    [Fact]
    public void IsValid_MultipleCallsInShortTime_ShouldReturnConsistentResults()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddSeconds(5);
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = futureTime
        };

        // Act
        var isValid1 = leaderInfo.IsValid;
        var isValid2 = leaderInfo.IsValid;
        var isValid3 = leaderInfo.IsValid;

        // Assert
        Assert.Equal(isValid1, isValid2);
        Assert.Equal(isValid2, isValid3);
        Assert.True(isValid1); // Should be valid for a few seconds
    }

    [Fact]
    public void TimeToExpiry_MultipleCallsInShortTime_ShouldReturnDecreasingValues()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddSeconds(5);
        var leaderInfo = new LeaderInfo
        {
            ParticipantId = DefaultParticipantId,
            AcquiredAt = _baseTime,
            ExpiresAt = futureTime
        };

        // Act
        var timeToExpiry1 = leaderInfo.TimeToExpiry;
        Thread.Sleep(100); // Wait 100ms
        var timeToExpiry2 = leaderInfo.TimeToExpiry;

        // Assert
        Assert.True(timeToExpiry1 > timeToExpiry2);
        Assert.True(timeToExpiry1 > TimeSpan.Zero);
        Assert.True(timeToExpiry2 > TimeSpan.Zero);
    }
}
