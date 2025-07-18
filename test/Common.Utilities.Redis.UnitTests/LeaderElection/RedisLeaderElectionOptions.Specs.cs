using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using NSubstitute;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.LeaderElection;

public class RedisLeaderElectionOptionsSpecs
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var options = new RedisLeaderElectionOptions();

        // Assert
        Assert.Null(options.ConnectionMultiplexer);
        Assert.Null(options.Serializer);
        Assert.Equal(-1, options.Database);
        Assert.Equal("leader_election", options.KeyPrefix);
    }

    [Fact]
    public void Properties_Should_Be_Settable()
    {
        // Arrange
        var connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        var serializer = Substitute.For<IDataSerializer>();
        const int database = 5;
        const string keyPrefix = "custom_prefix";

        var options = new RedisLeaderElectionOptions();

        // Act
        options.ConnectionMultiplexer = connectionMultiplexer;
        options.Serializer = serializer;
        options.Database = database;
        options.KeyPrefix = keyPrefix;

        // Assert
        Assert.Same(connectionMultiplexer, options.ConnectionMultiplexer);
        Assert.Same(serializer, options.Serializer);
        Assert.Equal(database, options.Database);
        Assert.Equal(keyPrefix, options.KeyPrefix);
    }

    [Fact]
    public void Validate_Should_Return_Same_Instance_When_Valid()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = Substitute.For<IDataSerializer>(),
            Database = 0,
            KeyPrefix = "test_prefix"
        };

        // Act
        var result = options.Validate();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void Validate_Should_Throw_When_ConnectionMultiplexer_Is_Null()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = null,
            Serializer = Substitute.For<IDataSerializer>(),
            KeyPrefix = "test_prefix"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Equal("ConnectionMultiplexer is required for Redis leader election.", exception.Message);
    }

    [Fact]
    public void Validate_Should_Throw_When_Serializer_Is_Null()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = null,
            KeyPrefix = "test_prefix"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Equal("Serializer is required for Redis leader election.", exception.Message);
    }

    [Fact]
    public void Validate_Should_Throw_When_KeyPrefix_Is_Null()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = Substitute.For<IDataSerializer>(),
            KeyPrefix = null!
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Equal("KeyPrefix cannot be null or whitespace.", exception.Message);
    }

    [Fact]
    public void Validate_Should_Throw_When_KeyPrefix_Is_Empty()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = Substitute.For<IDataSerializer>(),
            KeyPrefix = ""
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Equal("KeyPrefix cannot be null or whitespace.", exception.Message);
    }

    [Fact]
    public void Validate_Should_Throw_When_KeyPrefix_Is_Whitespace()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = Substitute.For<IDataSerializer>(),
            KeyPrefix = "   "
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Equal("KeyPrefix cannot be null or whitespace.", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    public void Database_Should_Accept_Valid_Database_Indexes(int databaseIndex)
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = Substitute.For<IDataSerializer>(),
            Database = databaseIndex,
            KeyPrefix = "test"
        };

        // Act & Assert - Should not throw
        var result = options.Validate();
        Assert.Equal(databaseIndex, result.Database);
    }

    [Fact]
    public void Validate_Should_Pass_With_Minimal_Valid_Configuration()
    {
        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = new ReflectionJsonDataSerializer()
        };

        // Act & Assert - Should not throw
        var result = options.Validate();
        Assert.NotNull(result);
        Assert.Equal("leader_election", result.KeyPrefix); // Default value should be preserved
        Assert.Equal(-1, result.Database); // Default value should be preserved
    }

    [Fact]
    public void Validate_Should_Pass_With_All_Properties_Set()
    {
        // Arrange
        var connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        var serializer = Substitute.For<IDataSerializer>();

        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = connectionMultiplexer,
            Serializer = serializer,
            Database = 3,
            KeyPrefix = "custom_election_prefix"
        };

        // Act
        var result = options.Validate();

        // Assert
        Assert.Same(options, result);
        Assert.Same(connectionMultiplexer, result.ConnectionMultiplexer);
        Assert.Same(serializer, result.Serializer);
        Assert.Equal(3, result.Database);
        Assert.Equal("custom_election_prefix", result.KeyPrefix);
    }

    [Fact]
    public void KeyPrefix_Should_Trim_Whitespace_For_Validation()
    {
        // Note: This test documents current behavior where whitespace-only strings fail validation
        // The actual implementation doesn't trim, it checks IsNullOrWhiteSpace directly

        // Arrange
        var options = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>(),
            Serializer = Substitute.For<IDataSerializer>(),
            KeyPrefix = " valid_prefix "
        };

        // Act & Assert - Should pass since it contains non-whitespace characters
        var result = options.Validate();
        Assert.Equal(" valid_prefix ", result.KeyPrefix); // Preserves the original value including spaces
    }

    [Fact]
    public void Validate_Should_Be_Chainable()
    {
        // Arrange
        var connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        var serializer = Substitute.For<IDataSerializer>();

        // Act - Test method chaining
        var result = new RedisLeaderElectionOptions
        {
            ConnectionMultiplexer = connectionMultiplexer,
            Serializer = serializer,
            Database = 2,
            KeyPrefix = "chained"
        }.Validate();

        // Assert
        Assert.NotNull(result);
        Assert.Same(connectionMultiplexer, result.ConnectionMultiplexer);
        Assert.Same(serializer, result.Serializer);
        Assert.Equal(2, result.Database);
        Assert.Equal("chained", result.KeyPrefix);
    }
}
