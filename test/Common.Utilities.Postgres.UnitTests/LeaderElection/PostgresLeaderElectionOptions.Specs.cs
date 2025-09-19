using AdaptArch.Common.Utilities.Postgres.LeaderElection;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using Npgsql;
using NSubstitute;

namespace AdaptArch.Common.Utilities.Postgres.UnitTests.LeaderElection;

public class PostgresLeaderElectionOptionsSpecs
{
    private readonly NpgsqlDataSource _mockDataSource;
    private readonly IStringDataSerializer _mockSerializer;

    public PostgresLeaderElectionOptionsSpecs()
    {
        _mockDataSource = NpgsqlDataSource.Create("Host=localhost;Database=testdb;Username=testuser;Password=testpassword");
        _mockSerializer = Substitute.For<IStringDataSerializer>();
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var options = new PostgresLeaderElectionOptions();

        // Assert
        Assert.Equal("leader_election_leases", options.TableName);
        Assert.True(options.AutoCreateTable);
        Assert.Equal(TimeSpan.FromMinutes(5), options.CleanupInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ConnectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), options.CommandTimeout);
        Assert.Null(options.DataSource);
        Assert.Null(options.Serializer);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions();
        const string customTableName = "custom_table";
        var customCleanupInterval = TimeSpan.FromMinutes(10);
        var customConnectionTimeout = TimeSpan.FromSeconds(60);
        var customCommandTimeout = TimeSpan.FromSeconds(45);

        // Act
        options.DataSource = _mockDataSource;
        options.Serializer = _mockSerializer;
        options.TableName = customTableName;
        options.AutoCreateTable = false;
        options.CleanupInterval = customCleanupInterval;
        options.ConnectionTimeout = customConnectionTimeout;
        options.CommandTimeout = customCommandTimeout;

        // Assert
        Assert.Equal(_mockDataSource, options.DataSource);
        Assert.Equal(_mockSerializer, options.Serializer);
        Assert.Equal(customTableName, options.TableName);
        Assert.False(options.AutoCreateTable);
        Assert.Equal(customCleanupInterval, options.CleanupInterval);
        Assert.Equal(customConnectionTimeout, options.ConnectionTimeout);
        Assert.Equal(customCommandTimeout, options.CommandTimeout);
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldReturnSameInstance()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            TableName = "valid_table",
            ConnectionTimeout = TimeSpan.FromSeconds(30),
            CommandTimeout = TimeSpan.FromSeconds(30),
            CleanupInterval = TimeSpan.FromMinutes(5)
        };

        // Act
        var result = options.Validate();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void Validate_WithNullDataSource_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = null,
            Serializer = _mockSerializer
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("DataSource is required", exception.Message);
    }

    [Fact]
    public void Validate_WithNullSerializer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = null
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("Serializer is required", exception.Message);
    }

    [Fact]
    public void Validate_WithNullTableName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            TableName = null!
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("TableName cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Validate_WithEmptyTableName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            TableName = ""
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("TableName cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("valid_table")]
    [InlineData("ValidTable")]
    [InlineData("valid-table")]
    [InlineData("_valid_table")]
    [InlineData("table123")]
    [InlineData("Table_123")]
    public void Validate_WithValidTableNames_ShouldNotThrow(string tableName)
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            TableName = tableName
        };

        // Act & Assert
        _ = options.Validate(); // Should not throw
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("123invalid")]
    [InlineData("table@name")]
    [InlineData("table name")]
    [InlineData("table.name")]
    [InlineData("table;name")]
    [InlineData("table'name")]
    [InlineData("table\"name")]
    public void Validate_WithInvalidTableNames_ShouldThrowInvalidOperationException(string tableName)
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            TableName = tableName
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("TableName contains invalid characters", exception.Message);
    }

    [Theory]
    [InlineData("")]
    public void Validate_WithInvalidTableNames_ShouldThrowInvalidOperationException_Whitespace(string tableName)
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            TableName = tableName
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("TableName cannot be null or empty.", exception.Message);
    }

    [Fact]
    public void Validate_WithZeroConnectionTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            ConnectionTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("ConnectionTimeout must be greater than zero", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeConnectionTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            ConnectionTimeout = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("ConnectionTimeout must be greater than zero", exception.Message);
    }

    [Fact]
    public void Validate_WithZeroCommandTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            CommandTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("CommandTimeout must be greater than zero", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeCommandTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            CommandTimeout = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("CommandTimeout must be greater than zero", exception.Message);
    }

    [Fact]
    public void Validate_WithZeroCleanupInterval_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            CleanupInterval = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("CleanupInterval must be greater than zero when specified", exception.Message);
    }

    [Fact]
    public void Validate_WithNegativeCleanupInterval_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            CleanupInterval = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("CleanupInterval must be greater than zero when specified", exception.Message);
    }

    [Fact]
    public void Validate_WithNullCleanupInterval_ShouldNotThrow()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            DataSource = _mockDataSource,
            Serializer = _mockSerializer,
            CleanupInterval = null
        };

        // Act & Assert
        _ = options.Validate(); // Should not throw
    }

    [Fact]
    public void CleanupInterval_CanBeSetToNull()
    {
        // Arrange
        var options = new PostgresLeaderElectionOptions
        {
            // Act
            CleanupInterval = null
        };

        // Assert
        Assert.Null(options.CleanupInterval);
    }
}
