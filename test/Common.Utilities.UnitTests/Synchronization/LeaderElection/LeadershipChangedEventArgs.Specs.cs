using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class LeadershipChangedEventArgsSpecs
{
    private readonly DateTime _baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string ParticipantId1 = "participant-1";
    private const string ParticipantId2 = "participant-2";

    private LeaderInfo CreateLeaderInfo(string participantId, DateTime? acquiredAt = null, DateTime? expiresAt = null)
    {
        return new LeaderInfo
        {
            ParticipantId = participantId,
            AcquiredAt = acquiredAt ?? _baseTime,
            ExpiresAt = expiresAt ?? _baseTime.AddMinutes(5)
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        const bool isLeader = true;
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);

        // Act
        var eventArgs = new LeadershipChangedEventArgs(isLeader, previousLeader, currentLeader);

        // Assert
        Assert.Equal(isLeader, eventArgs.IsLeader);
        Assert.Equal(previousLeader, eventArgs.PreviousLeader);
        Assert.Equal(currentLeader, eventArgs.CurrentLeader);
    }

    [Fact]
    public void Constructor_WithNullLeaders_ShouldInitializeCorrectly()
    {
        // Arrange
        const bool isLeader = false;

        // Act
        var eventArgs = new LeadershipChangedEventArgs(isLeader, null, null);

        // Assert
        Assert.Equal(isLeader, eventArgs.IsLeader);
        Assert.Null(eventArgs.PreviousLeader);
        Assert.Null(eventArgs.CurrentLeader);
    }

    [Fact]
    public void Constructor_WithNullPreviousLeader_ShouldInitializeCorrectly()
    {
        // Arrange
        const bool isLeader = true;
        var currentLeader = CreateLeaderInfo(ParticipantId1);

        // Act
        var eventArgs = new LeadershipChangedEventArgs(isLeader, null, currentLeader);

        // Assert
        Assert.Equal(isLeader, eventArgs.IsLeader);
        Assert.Null(eventArgs.PreviousLeader);
        Assert.Equal(currentLeader, eventArgs.CurrentLeader);
    }

    [Fact]
    public void Constructor_WithNullCurrentLeader_ShouldInitializeCorrectly()
    {
        // Arrange
        const bool isLeader = false;
        var previousLeader = CreateLeaderInfo(ParticipantId1);

        // Act
        var eventArgs = new LeadershipChangedEventArgs(isLeader, previousLeader, null);

        // Assert
        Assert.Equal(isLeader, eventArgs.IsLeader);
        Assert.Equal(previousLeader, eventArgs.PreviousLeader);
        Assert.Null(eventArgs.CurrentLeader);
    }

    [Fact]
    public void LeadershipGained_WhenBecameLeaderWithDifferentPrevious_ShouldReturnTrue()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.LeadershipGained);
    }

    [Fact]
    public void LeadershipGained_WhenBecameLeaderFromNoPrevious_ShouldReturnTrue()
    {
        // Arrange
        var currentLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(true, null, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.LeadershipGained);
    }

    [Fact]
    public void LeadershipGained_WhenBecameLeaderButSameAsPrevious_ShouldReturnFalse()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.LeadershipGained);
    }

    [Fact]
    public void LeadershipGained_WhenNotLeader_ShouldReturnFalse()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.LeadershipGained);
    }

    [Fact]
    public void LeadershipGained_WhenLeaderButNoCurrentLeader_ShouldReturnTrue()
    {
        // Arrange - This is an edge case where IsLeader=true but CurrentLeader=null
        // This could happen in transitional states or error conditions
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, null);

        // Act & Assert
        // Since PreviousLeader exists and CurrentLeader is null, they are different
        // and IsLeader is true, so LeadershipGained should be true
        Assert.True(eventArgs.LeadershipGained);
    }

    [Fact]
    public void LeadershipLost_WhenNotLeaderAndHadPrevious_ShouldReturnTrue()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.LeadershipLost);
    }

    [Fact]
    public void LeadershipLost_WhenNotLeaderAndNoPrevious_ShouldReturnFalse()
    {
        // Arrange
        var currentLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(false, null, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.LeadershipLost);
    }

    [Fact]
    public void LeadershipLost_WhenIsLeader_ShouldReturnFalse()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.LeadershipLost);
    }

    [Fact]
    public void LeadershipLost_WhenNotLeaderAndNoPreviousLeader_ShouldReturnFalse()
    {
        // Arrange
        var eventArgs = new LeadershipChangedEventArgs(false, null, null);

        // Act & Assert
        Assert.False(eventArgs.LeadershipLost);
    }

    [Fact]
    public void LeaderChanged_WhenDifferentParticipantIds_ShouldReturnTrue()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.LeaderChanged);
    }

    [Fact]
    public void LeaderChanged_WhenSameParticipantIds_ShouldReturnFalse()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.LeaderChanged);
    }

    [Fact]
    public void LeaderChanged_WhenPreviousNullAndCurrentExists_ShouldReturnTrue()
    {
        // Arrange
        var currentLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(true, null, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.LeaderChanged);
    }

    [Fact]
    public void LeaderChanged_WhenPreviousExistsAndCurrentNull_ShouldReturnTrue()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, null);

        // Act & Assert
        Assert.True(eventArgs.LeaderChanged);
    }

    [Fact]
    public void LeaderChanged_WhenBothNull_ShouldReturnFalse()
    {
        // Arrange
        var eventArgs = new LeadershipChangedEventArgs(false, null, null);

        // Act & Assert
        Assert.False(eventArgs.LeaderChanged);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert - Properties should be accessible but not settable after construction
        Assert.True(eventArgs.IsLeader);
        Assert.Equal(previousLeader, eventArgs.PreviousLeader);
        Assert.Equal(currentLeader, eventArgs.CurrentLeader);

        // Verify that properties are get-only (this is compile-time enforced)
        // The compiler would prevent: eventArgs.IsLeader = false;
    }

    [Fact]
    public void InheritsFromEventArgs_ShouldBeTrue()
    {
        // Arrange
        var eventArgs = new LeadershipChangedEventArgs(false, null, null);

        // Act & Assert
        Assert.IsType<EventArgs>(eventArgs, exactMatch: false);
    }

    [Theory]
    [InlineData(true, true, false, false)] // Became leader, different from previous
    [InlineData(true, false, true, false)] // Became leader, no previous leader
    [InlineData(false, true, false, true)] // Lost leadership
    [InlineData(false, false, false, false)] // Never was leader, no previous
    [InlineData(true, true, true, false)] // Still leader, same as previous
    public void ComputedProperties_ShouldReturnExpectedValues(
        bool isLeader,
        bool hasPrevious,
        bool hasCurrent,
        bool expectedLeadershipLost)
    {
        // Arrange
        var previousLeader = hasPrevious ? CreateLeaderInfo(ParticipantId1) : null;
        var currentLeader = hasCurrent ? CreateLeaderInfo(isLeader && hasPrevious ? ParticipantId1 : ParticipantId2) : null;
        var eventArgs = new LeadershipChangedEventArgs(isLeader, previousLeader, currentLeader);

        // Act & Assert
        if (expectedLeadershipLost)
            Assert.True(eventArgs.LeadershipLost);
        else
            Assert.False(eventArgs.LeadershipLost);

        var expectedLeadershipGained = isLeader &&
            (previousLeader?.ParticipantId != currentLeader?.ParticipantId);
        if (expectedLeadershipGained)
            Assert.True(eventArgs.LeadershipGained);
        else
            Assert.False(eventArgs.LeadershipGained);

        var expectedLeaderChanged = previousLeader?.ParticipantId != currentLeader?.ParticipantId;
        if (expectedLeaderChanged)
            Assert.True(eventArgs.LeaderChanged);
        else
            Assert.False(eventArgs.LeaderChanged);
    }

    [Fact]
    public void LeadershipGained_WithSameLeaderButDifferentLeaderInfoInstances_ShouldReturnFalse()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1, _baseTime, _baseTime.AddMinutes(5));
        var currentLeader = CreateLeaderInfo(ParticipantId1, _baseTime.AddMinutes(1), _baseTime.AddMinutes(6));
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.LeadershipGained);
        Assert.False(eventArgs.LeaderChanged);
    }

    [Fact]
    public void LeadershipScenarios_NewLeaderElected_ShouldHaveCorrectState()
    {
        // Arrange - A new leader is elected (first time)
        var currentLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(true, null, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.IsLeader);
        Assert.True(eventArgs.LeadershipGained);
        Assert.False(eventArgs.LeadershipLost);
        Assert.True(eventArgs.LeaderChanged);
        Assert.Null(eventArgs.PreviousLeader);
        Assert.NotNull(eventArgs.CurrentLeader);
    }

    [Fact]
    public void LeadershipScenarios_LeadershipTransferred_ShouldHaveCorrectState()
    {
        // Arrange - Leadership transferred from one participant to another
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.IsLeader);
        Assert.True(eventArgs.LeadershipGained);
        Assert.False(eventArgs.LeadershipLost);
        Assert.True(eventArgs.LeaderChanged);
        Assert.NotNull(eventArgs.PreviousLeader);
        Assert.NotNull(eventArgs.CurrentLeader);
        Assert.NotEqual(eventArgs.PreviousLeader.ParticipantId, eventArgs.CurrentLeader.ParticipantId);
    }

    [Fact]
    public void LeadershipScenarios_LeadershipLost_ShouldHaveCorrectState()
    {
        // Arrange - This participant lost leadership
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained);
        Assert.True(eventArgs.LeadershipLost);
        Assert.True(eventArgs.LeaderChanged);
        Assert.NotNull(eventArgs.PreviousLeader);
        Assert.NotNull(eventArgs.CurrentLeader);
    }

    [Fact]
    public void LeadershipScenarios_LeadershipExpired_ShouldHaveCorrectState()
    {
        // Arrange - Leadership expired, no new leader yet
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, null);

        // Act & Assert
        Assert.False(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained);
        Assert.True(eventArgs.LeadershipLost);
        Assert.True(eventArgs.LeaderChanged);
        Assert.NotNull(eventArgs.PreviousLeader);
        Assert.Null(eventArgs.CurrentLeader);
    }

    [Fact]
    public void LeadershipScenarios_LeadershipRenewed_ShouldHaveCorrectState()
    {
        // Arrange - Same leader renewed their lease
        var previousLeader = CreateLeaderInfo(ParticipantId1, _baseTime, _baseTime.AddMinutes(5));
        var currentLeader = CreateLeaderInfo(ParticipantId1, _baseTime, _baseTime.AddMinutes(10));
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert
        Assert.True(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained); // Same participant
        Assert.False(eventArgs.LeadershipLost);
        Assert.False(eventArgs.LeaderChanged); // Same participant ID
        Assert.NotNull(eventArgs.PreviousLeader);
        Assert.NotNull(eventArgs.CurrentLeader);
        Assert.Equal(eventArgs.PreviousLeader.ParticipantId, eventArgs.CurrentLeader.ParticipantId);
    }

    [Fact]
    public void LeadershipScenarios_ObserverViewOfLeaderChange_ShouldHaveCorrectState()
    {
        // Arrange - Observer sees leadership change between two other participants
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(false, previousLeader, currentLeader);

        // Act & Assert
        Assert.False(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained); // This participant didn't gain leadership
        Assert.True(eventArgs.LeadershipLost); // There was a previous leader and this participant is not the leader
        Assert.True(eventArgs.LeaderChanged);
        Assert.NotNull(eventArgs.PreviousLeader);
        Assert.NotNull(eventArgs.CurrentLeader);
    }

    [Fact]
    public void LeadershipScenarios_NoLeadershipChange_ShouldHaveCorrectState()
    {
        // Arrange - No leadership, no change
        var eventArgs = new LeadershipChangedEventArgs(false, null, null);

        // Act & Assert
        Assert.False(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained);
        Assert.False(eventArgs.LeadershipLost);
        Assert.False(eventArgs.LeaderChanged);
        Assert.Null(eventArgs.PreviousLeader);
        Assert.Null(eventArgs.CurrentLeader);
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulString()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act
        var stringRepresentation = eventArgs.ToString();

        // Assert
        Assert.NotNull(stringRepresentation);
        Assert.NotEmpty(stringRepresentation);
        // EventArgs base class provides default ToString implementation
    }

    [Fact]
    public void MultipleInstances_WithSameValues_ShouldNotBeEqual()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs1 = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);
        var eventArgs2 = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act & Assert - EventArgs instances are reference types, not value types
        Assert.NotEqual(eventArgs1, eventArgs2);
        Assert.False(ReferenceEquals(eventArgs1, eventArgs2));
    }

    [Fact]
    public void ComputedProperties_ShouldBeConsistentAcrossMultipleCalls()
    {
        // Arrange
        var previousLeader = CreateLeaderInfo(ParticipantId1);
        var currentLeader = CreateLeaderInfo(ParticipantId2);
        var eventArgs = new LeadershipChangedEventArgs(true, previousLeader, currentLeader);

        // Act
        var leadershipGained1 = eventArgs.LeadershipGained;
        var leadershipGained2 = eventArgs.LeadershipGained;
        var leadershipLost1 = eventArgs.LeadershipLost;
        var leadershipLost2 = eventArgs.LeadershipLost;
        var leaderChanged1 = eventArgs.LeaderChanged;
        var leaderChanged2 = eventArgs.LeaderChanged;

        // Assert
        Assert.True(leadershipGained1 == leadershipGained2);
        Assert.True(leadershipLost1 == leadershipLost2);
        Assert.True(leaderChanged1 == leaderChanged2);
    }
}
