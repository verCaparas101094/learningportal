namespace LearningPortal.Domain.Enrollments;

/// <summary>Defines the enrollment lifecycle.</summary>
public enum EnrollmentStatus
{
    /// <summary>The student is enrolled but has not started.</summary>
    Enrolled,
    /// <summary>The student has started the course.</summary>
    InProgress,
    /// <summary>The student completed the course.</summary>
    Completed,
    /// <summary>The student withdrew from the course.</summary>
    Withdrawn
}
