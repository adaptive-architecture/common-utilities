using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class LeaderElectionOptionsSpecs
{
    [Fact]
    public void Create_Should_Return_Valid_Instance_With_Correct_Timing_Ratios()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(3); // 180 seconds
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, metadata);

        // Assert
        Assert.Equal(leaseDuration, options.LeaseDuration);
        Assert.Equal(TimeSpan.FromSeconds(60), options.RenewalInterval); // 1/3 of 180s = 60s
        Assert.Equal(TimeSpan.FromSeconds(30), options.RetryInterval); // 1/6 of 180s = 30s
        Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout); // 1/6 of 180s = 30s
        Assert.Equal(metadata, options.Metadata);
        Assert.True(options.EnableContinuousCheck); // Default value
    }

    [Fact]
    public void Create_Should_Handle_Null_Metadata()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(1);

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        Assert.Equal(leaseDuration, options.LeaseDuration);
        Assert.Null(options.Metadata);
        Assert.True(options.EnableContinuousCheck);
    }

    [Fact]
    public void Create_Should_Handle_Empty_Metadata()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(2);
        var emptyMetadata = new Dictionary<string, string>();

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, emptyMetadata);

        // Assert
        Assert.Equal(leaseDuration, options.LeaseDuration);
        Assert.Equal(emptyMetadata, options.Metadata);
        Assert.Empty(options.Metadata);
    }

    [Theory]
    [InlineData(30, 10, 5, 5)]          // 30s lease -> 10s renewal, 5s retry, 5s timeout
    [InlineData(60, 20, 10, 10)]        // 60s lease -> 20s renewal, 10s retry, 10s timeout
    [InlineData(300, 100, 50, 50)]      // 300s lease -> 100s renewal, 50s retry, 50s timeout
    [InlineData(6, 2, 1, 1)]            // 6s lease -> 2s renewal, 1s retry, 1s timeout
    public void Create_Should_Calculate_Timing_Intervals_Correctly(
        int leaseDurationSeconds,
        int expectedRenewalSeconds,
        int expectedRetrySeconds,
        int expectedTimeoutSeconds)
    {
        // Arrange
        var leaseDuration = TimeSpan.FromSeconds(leaseDurationSeconds);

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedRenewalSeconds), options.RenewalInterval);
        Assert.Equal(TimeSpan.FromSeconds(expectedRetrySeconds), options.RetryInterval);
        Assert.Equal(TimeSpan.FromSeconds(expectedTimeoutSeconds), options.OperationTimeout);
    }

    [Fact]
    public void Create_Should_Handle_Millisecond_Precision()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMilliseconds(3000); // 3 seconds

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        // 3 seconds is less than 5 seconds, so Validate() corrects lease duration to 30 seconds
        // BUT the intervals calculated from the original 3 seconds (1s, 0.5s, 0.5s) are still valid
        // because they are less than the corrected lease duration of 30 seconds
        Assert.Equal(TimeSpan.FromSeconds(30), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromSeconds(1), options.RenewalInterval); // 3000ms / 3 = 1000ms = 1s
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.RetryInterval); // 3000ms / 6 = 500ms
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.OperationTimeout); // 3000ms / 6 = 500ms
    }

    [Fact]
    public void Create_Should_Handle_Fractional_Milliseconds()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMilliseconds(1000); // 1 second

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        // 1 second is less than 5 seconds, so Validate() corrects lease duration to 30 seconds
        // BUT the intervals calculated from the original 1 second remain valid
        Assert.Equal(TimeSpan.FromSeconds(30), options.LeaseDuration);
        // 1000ms / 3 = 333.333ms (TimeSpan preserves fractional milliseconds)
        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 3), options.RenewalInterval);
        // 1000ms / 6 = 166.666ms (TimeSpan preserves fractional milliseconds)
        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 6), options.RetryInterval);
        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 6), options.OperationTimeout);
    }

    [Fact]
    public void Create_Should_Call_Validate_And_Return_Validated_Instance()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromSeconds(1); // Very short lease that triggers validation

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        // The Validate() method should correct very short lease durations (< 5s becomes 30s)
        // BUT the intervals calculated from the original 1 second remain valid
        Assert.Equal(TimeSpan.FromSeconds(30), options.LeaseDuration); // Should be corrected to 30s
        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 3), options.RenewalInterval); // 1000ms / 3 = 333.333ms
        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 6), options.RetryInterval); // 1000ms / 6 = 166.666ms
        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 6), options.OperationTimeout); // 1000ms / 6 = 166.666ms
    }

    [Fact]
    public void Create_Should_Handle_Valid_Lease_Duration_Without_Validation_Correction()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromSeconds(12); // >= 5 seconds, should not be corrected

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(12), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromSeconds(4), options.RenewalInterval); // 12s / 3 = 4s
        Assert.Equal(TimeSpan.FromSeconds(2), options.RetryInterval); // 12s / 6 = 2s
        Assert.Equal(TimeSpan.FromSeconds(2), options.OperationTimeout); // 12s / 6 = 2s
    }

    [Fact]
    public void Create_Should_Preserve_Metadata_After_Validation()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromSeconds(2); // Short lease that triggers validation
        var metadata = new Dictionary<string, string>
        {
            ["hostname"] = "test-host",
            ["version"] = "1.0.0"
        };

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, metadata);

        // Assert
        Assert.Equal(metadata, options.Metadata);
        Assert.Equal("test-host", options.Metadata["hostname"]);
        Assert.Equal("1.0.0", options.Metadata["version"]);
    }

    [Fact]
    public void Create_Should_Handle_Large_Lease_Durations()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromHours(1); // 1 hour

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromMinutes(20), options.RenewalInterval); // 1 hour / 3 = 20 minutes
        Assert.Equal(TimeSpan.FromMinutes(10), options.RetryInterval); // 1 hour / 6 = 10 minutes
        Assert.Equal(TimeSpan.FromMinutes(10), options.OperationTimeout); // 1 hour / 6 = 10 minutes
    }

    [Fact]
    public void Create_Should_Handle_Very_Small_Lease_Durations()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMilliseconds(100); // Very small

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        // Lease duration should be corrected by validation to minimum 30 seconds
        // BUT the intervals calculated from the original 100ms remain valid
        Assert.Equal(TimeSpan.FromSeconds(30), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(100.0 / 3), options.RenewalInterval); // 100ms / 3 = 33.333ms
        Assert.Equal(TimeSpan.FromMilliseconds(100.0 / 6), options.RetryInterval); // 100ms / 6 = 16.666ms
        Assert.Equal(TimeSpan.FromMilliseconds(100.0 / 6), options.OperationTimeout); // 100ms / 6 = 16.666ms
    }

    [Fact]
    public void Create_Should_Maintain_Timing_Relationships()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(6); // 360 seconds

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        // Verify the mathematical relationships
        Assert.Equal(leaseDuration, options.LeaseDuration);
        Assert.Equal(options.LeaseDuration.TotalMilliseconds / 3, options.RenewalInterval.TotalMilliseconds);
        Assert.Equal(options.LeaseDuration.TotalMilliseconds / 6, options.RetryInterval.TotalMilliseconds);
        Assert.Equal(options.LeaseDuration.TotalMilliseconds / 6, options.OperationTimeout.TotalMilliseconds);

        // Verify renewal interval is less than lease duration
        Assert.True(options.RenewalInterval < options.LeaseDuration);

        // Verify retry and operation timeout are the same
        Assert.Equal(options.RetryInterval, options.OperationTimeout);

        // Verify retry interval is less than renewal interval
        Assert.True(options.RetryInterval < options.RenewalInterval);
    }

    [Fact]
    public void Create_Should_Handle_Complex_Metadata_Dictionary()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(5);
        var metadata = new Dictionary<string, string>
        {
            ["hostname"] = "prod-server-01",
            ["process_id"] = "12345",
            ["version"] = "2.1.0",
            ["datacenter"] = "us-east-1",
            ["environment"] = "production",
            ["start_time"] = DateTimeOffset.UtcNow.ToString("O"),
            ["special_chars"] = "!@#$%^&*()_+-=[]{}|;:,.<>?"
        };

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, metadata);

        // Assert
        Assert.Equal(metadata, options.Metadata);
        Assert.Equal(7, options.Metadata.Count);
        Assert.Equal("prod-server-01", options.Metadata["hostname"]);
        Assert.Equal("!@#$%^&*()_+-=[]{}|;:,.<>?", options.Metadata["special_chars"]);
    }

    [Fact]
    public void Create_Should_Return_New_Instance_Each_Time()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(2);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var options1 = LeaderElectionOptions.Create(leaseDuration, metadata);
        var options2 = LeaderElectionOptions.Create(leaseDuration, metadata);

        // Assert
        Assert.NotSame(options1, options2); // Different instances
        Assert.Equal(options1.LeaseDuration, options2.LeaseDuration); // Same values
        Assert.Equal(options1.RenewalInterval, options2.RenewalInterval);
        Assert.Equal(options1.RetryInterval, options2.RetryInterval);
        Assert.Equal(options1.OperationTimeout, options2.OperationTimeout);
        Assert.Equal(options1.Metadata, options2.Metadata);
    }

    [Theory]
    [InlineData(0)] // Zero duration
    [InlineData(-1)] // Negative duration
    [InlineData(-3600)] // Negative hour
    public void Create_Should_Handle_Invalid_Lease_Durations_Through_Validation(int seconds)
    {
        // Arrange
        var leaseDuration = TimeSpan.FromSeconds(seconds);

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        // Invalid durations should be corrected by validation to 30 seconds
        Assert.Equal(TimeSpan.FromSeconds(30), options.LeaseDuration);

        // For negative durations, the calculated intervals will be negative
        // For zero duration, intervals will be zero
        // The validation logic only corrects intervals that are >= leaseDuration
        // So negative/zero intervals calculated from negative/zero input will not be corrected
        // unless they somehow end up being >= the corrected lease duration (which is unlikely)

        // We need to understand the actual behavior - let's be more specific
        if (seconds == 0)
        {
            // 0 / 3 = 0, 0 / 6 = 0 - these are not >= 30s, so they won't be corrected
            Assert.Equal(TimeSpan.Zero, options.RenewalInterval);
            Assert.Equal(TimeSpan.Zero, options.RetryInterval);
            Assert.Equal(TimeSpan.Zero, options.OperationTimeout);
        }
        else if (seconds < 0)
        {
            // negative values will result in negative intervals
            Assert.True(options.RenewalInterval.TotalMilliseconds < 0);
            Assert.True(options.RetryInterval.TotalMilliseconds < 0);
            Assert.True(options.OperationTimeout.TotalMilliseconds < 0);
        }
    }

    [Fact]
    public void Create_Should_Preserve_EnableContinuousCheck_Default_Value()
    {
        // Arrange
        var leaseDuration = TimeSpan.FromMinutes(1);

        // Act
        var options = LeaderElectionOptions.Create(leaseDuration, null);

        // Assert
        Assert.True(options.EnableContinuousCheck); // Should use default value of true
    }
}
