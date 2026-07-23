using LearningPortal.Domain.Learning;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Learning;

/// <summary>Verifies lesson progress lifecycle invariants.</summary>
public sealed class LessonProgressDomainTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 7, 23, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies first access creates in-progress state.</summary>
    [Fact]
    public void Start_CreatesInProgressProgress()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StartedAt);
        Assert.Equal(LessonProgressStatus.InProgress, progress.Status);
        Assert.Equal(StartedAt, progress.StartedAtUtc);
        Assert.Equal(StartedAt, progress.LastAccessedAtUtc);
    }

    /// <summary>Verifies repeated access updates access time.</summary>
    [Fact]
    public void Access_UpdatesLastAccessedAtUtc()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StartedAt);
        var accessedAt = StartedAt.AddMinutes(1);
        progress.Access(accessedAt);
        Assert.Equal(accessedAt, progress.LastAccessedAtUtc);
    }

    /// <summary>Verifies completion is idempotent and cannot regress.</summary>
    [Fact]
    public void Complete_IsIdempotentAndAccessDoesNotRegress()
    {
        var progress = LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), StartedAt);
        var completedAt = StartedAt.AddMinutes(1);
        progress.Complete(completedAt);
        progress.Complete(completedAt.AddMinutes(1));
        progress.Access(completedAt.AddMinutes(2));
        Assert.Equal(LessonProgressStatus.Completed, progress.Status);
        Assert.Equal(completedAt, progress.CompletedAtUtc);
    }

    /// <summary>Verifies domain timestamps reject non-UTC values.</summary>
    [Fact]
    public void Start_RejectsNonUtcTimestamp() => Assert.Throws<ArgumentException>(() =>
        LessonProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.FromHours(8))));
}
