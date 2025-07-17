namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

/// <summary>
/// Event arguments for leadership change events.
/// </summary>
public sealed class LeadershipChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeadershipChangedEventArgs"/> class.
    /// </summary>
    /// <param name="isLeader">Whether this participant is now the leader.</param>
    /// <param name="previousLeader">Information about the previous leader, if any.</param>
    /// <param name="currentLeader">Information about the current leader, if any.</param>
    public LeadershipChangedEventArgs(bool isLeader, LeaderInfo? previousLeader, LeaderInfo? currentLeader)
    {
        IsLeader = isLeader;
        PreviousLeader = previousLeader;
        CurrentLeader = currentLeader;
    }

    /// <summary>
    /// Gets a value indicating whether this participant is now the leader.
    /// </summary>
    public bool IsLeader { get; }

    /// <summary>
    /// Gets information about the previous leader, if any.
    /// </summary>
    public LeaderInfo? PreviousLeader { get; }

    /// <summary>
    /// Gets information about the current leader, if any.
    /// </summary>
    public LeaderInfo? CurrentLeader { get; }

    /// <summary>
    /// Gets a value indicating whether leadership was gained.
    /// </summary>
    public bool LeadershipGained => IsLeader && PreviousLeader?.ParticipantId != CurrentLeader?.ParticipantId;

    /// <summary>
    /// Gets a value indicating whether leadership was lost.
    /// </summary>
    public bool LeadershipLost => !IsLeader && PreviousLeader != null;

    /// <summary>
    /// Gets a value indicating whether there was a leader change (but not necessarily involving this participant).
    /// </summary>
    public bool LeaderChanged => PreviousLeader?.ParticipantId != CurrentLeader?.ParticipantId;
}
