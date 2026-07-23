namespace LearningPortal.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Coordinates two refresh requests after both have loaded the original token.
/// </summary>
public sealed class RefreshRotationCoordinator
{
    private TaskCompletionSource? _release;
    private int _remainingParticipants;

    /// <summary>Arms the coordinator for the expected number of refresh requests.</summary>
    public void Arm(int participantCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(participantCount, 2);

        _remainingParticipants = participantCount;
        _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Releases every participant after all expected requests reach the barrier.</summary>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        var release = Volatile.Read(ref _release);
        if (release is null)
        {
            return;
        }

        if (Interlocked.Decrement(ref _remainingParticipants) == 0)
        {
            release.TrySetResult();
        }

        await release.Task.WaitAsync(cancellationToken);
    }

    /// <summary>Disables coordination after a concurrent refresh test finishes.</summary>
    public void Disarm()
    {
        _release = null;
        _remainingParticipants = 0;
    }
}
