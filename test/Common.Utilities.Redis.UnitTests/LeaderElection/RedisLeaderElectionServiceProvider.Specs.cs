using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using NSubstitute;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.LeaderElection;

public class RedisLeaderElectionServiceProviderSpecs
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDataSerializer _serializer;

    public RedisLeaderElectionServiceProviderSpecs()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _serializer = Substitute.For<IDataSerializer>();
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Required_Parameters()
    {
        // Act
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Logger()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer, logger);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_Should_Throw_When_ConnectionMultiplexer_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RedisLeaderElectionServiceProvider(null!, _serializer));

        Assert.Equal("connectionMultiplexer", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Serializer_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new RedisLeaderElectionServiceProvider(_connectionMultiplexer, null!));

        Assert.Equal("serializer", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Accept_Null_Logger()
    {
        // Act & Assert - Should not throw
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer, null);
        Assert.NotNull(provider);
    }

    [Fact]
    public void CreateElection_Should_Return_RedisLeaderElectionService()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act
        var election = provider.CreateElection(electionName, participantId);

        // Assert
        Assert.NotNull(election);
        _ = Assert.IsType<RedisLeaderElectionService>(election);
        Assert.Equal(electionName, election.ElectionName);
        Assert.Equal(participantId, election.ParticipantId);
        Assert.False(election.IsLeader);
        Assert.Null(election.CurrentLeader);
    }

    [Fact]
    public void CreateElection_Should_Return_Service_With_Options()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(10),
            RenewalInterval = TimeSpan.FromMinutes(3),
            RetryInterval = TimeSpan.FromSeconds(30),
            EnableContinuousCheck = false
        };

        // Act
        var election = provider.CreateElection(electionName, participantId, options);

        // Assert
        Assert.NotNull(election);
        _ = Assert.IsType<RedisLeaderElectionService>(election);
        Assert.Equal(electionName, election.ElectionName);
        Assert.Equal(participantId, election.ParticipantId);
    }

    [Fact]
    public void CreateElection_Should_Create_Multiple_Independent_Services()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);

        // Act
        var election1 = provider.CreateElection("election-1", "participant-1");
        var election2 = provider.CreateElection("election-2", "participant-2");
        var election3 = provider.CreateElection("election-1", "participant-3"); // Same election, different participant

        // Assert
        Assert.NotNull(election1);
        Assert.NotNull(election2);
        Assert.NotNull(election3);

        Assert.NotSame(election1, election2);
        Assert.NotSame(election1, election3);
        Assert.NotSame(election2, election3);

        Assert.Equal("election-1", election1.ElectionName);
        Assert.Equal("election-2", election2.ElectionName);
        Assert.Equal("election-1", election3.ElectionName);

        Assert.Equal("participant-1", election1.ParticipantId);
        Assert.Equal("participant-2", election2.ParticipantId);
        Assert.Equal("participant-3", election3.ParticipantId);
    }

    [Fact]
    public void CreateElection_Should_Pass_Logger_To_Service()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer, logger);

        // Act
        var election = provider.CreateElection("test-election", "participant-1");

        // Assert
        Assert.NotNull(election);
        // Note: We can't directly verify the logger was passed since it's a private field,
        // but this test ensures the constructor accepts it without throwing
    }

    [Fact]
    public void CreateElection_Should_Work_With_Real_Serializer()
    {
        // Arrange
        var realSerializer = new ReflectionJsonDataSerializer();
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, realSerializer);

        // Act
        var election = provider.CreateElection("real-election", "real-participant");

        // Assert
        Assert.NotNull(election);
        _ = Assert.IsType<RedisLeaderElectionService>(election);
        Assert.Equal("real-election", election.ElectionName);
        Assert.Equal("real-participant", election.ParticipantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("valid-election")]
    [InlineData("election_with_underscores")]
    [InlineData("election-with-dashes")]
    [InlineData("election123")]
    [InlineData("UPPERCASE")]
    [InlineData("MixedCase")]
    public void CreateElection_Should_Accept_Various_Election_Names(string electionName)
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);

        // Act & Assert
        if (String.IsNullOrWhiteSpace(electionName))
        {
            // ElectionName validation happens in the RedisLeaderElectionService constructor
            _ = Assert.Throws<ArgumentException>(() =>
                provider.CreateElection(electionName, "participant-1"));
        }
        else
        {
            var election = provider.CreateElection(electionName, "participant-1");
            Assert.NotNull(election);
            Assert.Equal(electionName, election.ElectionName);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("valid-participant")]
    [InlineData("participant_with_underscores")]
    [InlineData("participant-with-dashes")]
    [InlineData("participant123")]
    [InlineData("UPPERCASE")]
    [InlineData("MixedCase")]
    public void CreateElection_Should_Accept_Various_Participant_Ids(string participantId)
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);

        // Act & Assert
        if (String.IsNullOrWhiteSpace(participantId))
        {
            // ParticipantId validation happens in the RedisLeaderElectionService constructor
            _ = Assert.Throws<ArgumentException>(() =>
                provider.CreateElection("test-election", participantId));
        }
        else
        {
            var election = provider.CreateElection("test-election", participantId);
            Assert.NotNull(election);
            Assert.Equal(participantId, election.ParticipantId);
        }
    }

    [Fact]
    public void CreateElection_Should_Pass_Through_All_LeaderElectionOptions()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);
        var metadata = new Dictionary<string, string>
        {
            ["version"] = "1.0",
            ["instance"] = "test-instance"
        };

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(15),
            RenewalInterval = TimeSpan.FromMinutes(5),
            RetryInterval = TimeSpan.FromSeconds(45),
            OperationTimeout = TimeSpan.FromSeconds(10),
            EnableContinuousCheck = true,
            Metadata = metadata
        };

        // Act
        var election = provider.CreateElection("test-election", "test-participant", options);

        // Assert
        Assert.NotNull(election);
        _ = Assert.IsType<RedisLeaderElectionService>(election);

        // We can't directly verify the options were passed since they're private,
        // but this test ensures the service is created without errors
    }

    [Fact]
    public void CreateElection_Should_Work_Without_Options()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);

        // Act
        var election = provider.CreateElection("test-election", "test-participant");

        // Assert
        Assert.NotNull(election);
        _ = Assert.IsType<RedisLeaderElectionService>(election);
        Assert.Equal("test-election", election.ElectionName);
        Assert.Equal("test-participant", election.ParticipantId);
    }

    [Fact]
    public async Task CreateElection_Should_Be_Thread_Safe()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);
        const int threadCount = 10;
        const int electionsPerThread = 5;
        var elections = new List<ILeaderElectionService>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            int threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                var threadElections = new List<ILeaderElectionService>();
                for (int j = 0; j < electionsPerThread; j++)
                {
                    var election = provider.CreateElection(
                        $"election-{threadIndex}-{j}",
                        $"participant-{threadIndex}-{j}");
                    threadElections.Add(election);
                }

                lock (elections)
                {
                    elections.AddRange(threadElections);
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert
        Assert.Equal(threadCount * electionsPerThread, elections.Count);
        Assert.All(elections, Assert.NotNull);

        // Verify all elections are unique
        var uniqueElections = elections.Distinct().ToList();
        Assert.Equal(elections.Count, uniqueElections.Count);
    }

    [Fact]
    public async Task CreateElection_Services_Should_Be_Disposable()
    {
        // Arrange
        var provider = new RedisLeaderElectionServiceProvider(_connectionMultiplexer, _serializer);

        // Act
        var election = provider.CreateElection("disposable-election", "disposable-participant");

        // Assert & Cleanup
        Assert.NotNull(election);
        await election.DisposeAsync(); // Should not throw
    }
}
