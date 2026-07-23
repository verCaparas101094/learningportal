using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Learning;

/// <summary>Tracks one student's progress through one lesson in an enrollment.</summary>
public sealed class LessonProgress : AuditableEntity
{
    private LessonProgress() { }
    private LessonProgress(Guid enrollmentId, Guid lessonId, Guid studentId, DateTimeOffset accessedAtUtc)
    {
        EnrollmentId = enrollmentId; LessonId = lessonId; StudentId = studentId;
        Status = LessonProgressStatus.InProgress; StartedAtUtc = accessedAtUtc; LastAccessedAtUtc = accessedAtUtc;
    }
    /// <summary>Gets the owning enrollment.</summary>
    public Guid EnrollmentId { get; private set; }
    /// <summary>Gets the lesson being tracked.</summary>
    public Guid LessonId { get; private set; }
    /// <summary>Gets the learner.</summary>
    public Guid StudentId { get; private set; }
    /// <summary>Gets the current progress state.</summary>
    public LessonProgressStatus Status { get; private set; }
    /// <summary>Gets when access first began.</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }
    /// <summary>Gets when completion occurred.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    /// <summary>Gets the most recent access time.</summary>
    public DateTimeOffset LastAccessedAtUtc { get; private set; }

    /// <summary>Creates initially in-progress learner progress.</summary>
    public static LessonProgress Start(Guid enrollmentId, Guid lessonId, Guid studentId, DateTimeOffset accessedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(enrollmentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(lessonId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentId, Guid.Empty);
        EnsureUtc(accessedAtUtc);
        return new(enrollmentId, lessonId, studentId, accessedAtUtc);
    }
    /// <summary>Records access without regressing completed progress.</summary>
    public void Access(DateTimeOffset accessedAtUtc)
    {
        EnsureUtc(accessedAtUtc);
        if (Status != LessonProgressStatus.Completed) { Status = LessonProgressStatus.InProgress; StartedAtUtc ??= accessedAtUtc; }
        LastAccessedAtUtc = accessedAtUtc;
    }
    /// <summary>Completes progress idempotently.</summary>
    public void Complete(DateTimeOffset completedAtUtc)
    {
        EnsureUtc(completedAtUtc);
        if (Status == LessonProgressStatus.Completed) return;
        StartedAtUtc ??= completedAtUtc; LastAccessedAtUtc = completedAtUtc; CompletedAtUtc = completedAtUtc;
        Status = LessonProgressStatus.Completed;
    }
    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("The timestamp must use UTC.", nameof(value));
    }
}
