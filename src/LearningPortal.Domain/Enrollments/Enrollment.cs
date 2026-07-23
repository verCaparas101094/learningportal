using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Enrollments;

/// <summary>Represents one student's enrollment in a course.</summary>
public sealed class Enrollment : AuditableEntity
{
    private Enrollment()
    {
    }

    private Enrollment(Guid courseId, Guid studentId, DateTimeOffset enrolledAtUtc)
    {
        CourseId = courseId;
        StudentId = studentId;
        Status = EnrollmentStatus.Enrolled;
        EnrolledAtUtc = enrolledAtUtc;
    }

    /// <summary>Gets the course identifier.</summary>
    public Guid CourseId { get; private set; }
    /// <summary>Gets the student identifier.</summary>
    public Guid StudentId { get; private set; }
    /// <summary>Gets the lifecycle status.</summary>
    public EnrollmentStatus Status { get; private set; }
    /// <summary>Gets the enrollment timestamp.</summary>
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    /// <summary>Gets the first-start timestamp.</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }
    /// <summary>Gets the completion timestamp.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    /// <summary>Gets the withdrawal timestamp.</summary>
    public DateTimeOffset? WithdrawnAtUtc { get; private set; }

    /// <summary>Creates an enrollment.</summary>
    public static Enrollment Create(Guid courseId, Guid studentId, DateTimeOffset enrolledAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(courseId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentId, Guid.Empty);
        EnsureUtc(enrolledAtUtc);
        return new Enrollment(courseId, studentId, enrolledAtUtc);
    }

    /// <summary>Starts an enrolled course.</summary>
    public bool TryStart(DateTimeOffset occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);
        if (Status != EnrollmentStatus.Enrolled)
        {
            return false;
        }

        Status = EnrollmentStatus.InProgress;
        StartedAtUtc = occurredAtUtc;
        return true;
    }

    /// <summary>Completes an enrolled or in-progress course.</summary>
    public bool TryComplete(DateTimeOffset occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);
        if (Status is not (EnrollmentStatus.Enrolled or EnrollmentStatus.InProgress))
        {
            return false;
        }

        StartedAtUtc ??= occurredAtUtc;
        CompletedAtUtc = occurredAtUtc;
        Status = EnrollmentStatus.Completed;
        return true;
    }

    /// <summary>Withdraws an enrolled or in-progress course.</summary>
    public bool TryWithdraw(DateTimeOffset occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc);
        if (Status is not (EnrollmentStatus.Enrolled or EnrollmentStatus.InProgress))
        {
            return false;
        }

        WithdrawnAtUtc = occurredAtUtc;
        Status = EnrollmentStatus.Withdrawn;
        return true;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The timestamp must use UTC.", nameof(value));
        }
    }
}
