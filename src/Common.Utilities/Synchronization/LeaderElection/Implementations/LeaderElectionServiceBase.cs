using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations;

/// <summary>
/// Base implementation of leader election service using lease-based coordination.
/// </summary>
public abstract class LeaderElectionServiceBase : ILeaderElectionService
{
    private readonly ILeaseStore _leaseStore;
    private readonly ILogger _logger;
    private readonly LeaderElectionOptions _options;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private Task? _electionTask;
    private volatile bool _isLeader;
    private volatile bool _isDisposed;
    private LeaderInfo? _currentLeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderElectionServiceBase"/> class.
    /// </summary>
    /// <param name="leaseStore">The lease store to use for coordination.</param>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    protected LeaderElectionServiceBase(
        ILeaseStore leaseStore,
        string electionName,
        string participantId,
        LeaderElectionOptions? options,
        ILogger? logger = null)
    {
        _leaseStore = leaseStore ?? throw new ArgumentNullException(nameof(leaseStore));
        _logger = logger ?? NullLogger.Instance;

        ElectionName = !String.IsNullOrWhiteSpace(electionName)
            ? electionName
            : throw new ArgumentException("Election name cannot be null or whitespace.", nameof(electionName));

        ParticipantId = !String.IsNullOrWhiteSpace(participantId)
            ? participantId
            : throw new ArgumentException("Participant ID cannot be null or whitespace.", nameof(participantId));

        _options = options?.Validate() ?? new LeaderElectionOptions();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <inheritdoc/>
    public string ParticipantId { get; }

    /// <inheritdoc/>
    public string ElectionName { get; }

    /// <inheritdoc/>
    public bool IsLeader => _isLeader;

    /// <inheritdoc/>
    public LeaderInfo? CurrentLeader => _currentLeader;

    /// <inheritdoc/>
    public event EventHandler<LeadershipChangedEventArgs>? LeadershipChanged;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_electionTask != null)
        {
            _logger.LogWarning(null, "Leader election is already running for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting leader election for {ElectionName}:{ParticipantId}",
            ElectionName, ParticipantId);

        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token).Token;

        if (_options.EnableContinuousCheck)
        {
            _electionTask = RunElectionLoopAsync(combinedToken);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_isLeader)
            {
                await ReleaseLeadershipAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_electionTask == null)
                return;

            _logger.LogInformation("Stopping leader election for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);

            _cancellationTokenSource.Cancel();

            await _electionTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("Leader election stopped for {ElectionName}:{ParticipantId} due to cancellation",
                ElectionName, ParticipantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop leader election for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);
        }

        _electionTask = null;
    }

    /// <inheritdoc/>
    public async Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var timeoutCts = new CancellationTokenSource(_options.OperationTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            var lease = await _leaseStore.TryAcquireLeaseAsync(
                ElectionName,
                ParticipantId,
                _options.LeaseDuration,
                _options.Metadata,
                combinedCts.Token).ConfigureAwait(false);

            if (lease == null)
            {
                lease = await _leaseStore.GetCurrentLeaseAsync(ElectionName, combinedCts.Token)
                    .ConfigureAwait(false);

                var isLeader = lease?.ParticipantId == ParticipantId;
                UpdateLeadershipStatus(isLeader, lease);
                return isLeader;
            }
            else
            {
                UpdateLeadershipStatus(true, lease);
                return true;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire leadership for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default)
    {
        if (!_isLeader)
            return;

        try
        {
            using var timeoutCts = new CancellationTokenSource(_options.OperationTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            var wasReleased = await _leaseStore.ReleaseLeaseAsync(ElectionName, ParticipantId, combinedCts.Token)
                .ConfigureAwait(false);

            UpdateLeadershipStatus(false, null);

            _logger.LogInformation("ReleaseLeaseAsync returned {WasRelease}, but updating local state anyway for {ElectionName}:{ParticipantId}",
                wasReleased, ElectionName, ParticipantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release leadership for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);
            throw;
        }
    }

    private async Task RunElectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_isLeader)
                {
                    await RenewLeadershipAsync(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(_options.RenewalInterval, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await TryAcquireLeadershipAsync(cancellationToken).ConfigureAwait(false);
                    await CheckCurrentLeaderAsync(cancellationToken).ConfigureAwait(false);
                    await Task.Delay(_options.RetryInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in election loop for {ElectionName}:{ParticipantId}",
                    ElectionName, ParticipantId);

                // Back off on errors
                await Task.Delay(_options.RetryInterval, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RenewLeadershipAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(_options.OperationTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            var lease = await _leaseStore.TryRenewLeaseAsync(
                ElectionName,
                ParticipantId,
                _options.LeaseDuration,
                _options.Metadata,
                combinedCts.Token).ConfigureAwait(false);

            if (lease == null)
            {
                lease = await _leaseStore.GetCurrentLeaseAsync(ElectionName, combinedCts.Token)
                    .ConfigureAwait(false);
                UpdateLeadershipStatus(false, lease);
            }
            else
            {
                UpdateLeadershipStatus(true, lease);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing leadership for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);
            UpdateLeadershipStatus(false, null);
        }
    }

    private async Task CheckCurrentLeaderAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.OperationTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        var newLease = await _leaseStore.GetCurrentLeaseAsync(ElectionName, combinedCts.Token)
            .ConfigureAwait(false);

        var isLeader = newLease?.ParticipantId == ParticipantId;
        UpdateLeadershipStatus(isLeader, newLease);
    }

    private void UpdateLeadershipStatus(bool isLeader, LeaderInfo? leaderInfo)
    {
        var wasLeader = _isLeader;
        var previousLeader = _currentLeader;

        _isLeader = isLeader;
        _currentLeader = leaderInfo;

        if (wasLeader != isLeader)
        {
            var eventArgs = new LeadershipChangedEventArgs(isLeader, previousLeader, leaderInfo);
            OnLeadershipChanged(eventArgs);
        }
    }

    /// <summary>
    /// Raises the <see cref="LeadershipChanged"/> event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnLeadershipChanged(LeadershipChangedEventArgs e)
    {
        try
        {
            LeadershipChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in leadership changed event handler for {ElectionName}:{ParticipantId}",
                ElectionName, ParticipantId);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(LeaderElectionServiceBase));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="LeaderElectionServiceBase"/> and optionally releases the managed resources.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_isDisposed)
        {
            await StopAsync().ConfigureAwait(false);

            _cancellationTokenSource.Dispose();
            _isDisposed = true;
        }
    }
}
