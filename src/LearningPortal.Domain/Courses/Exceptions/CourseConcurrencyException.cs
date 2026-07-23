namespace LearningPortal.Domain.Courses.Exceptions;

/// <summary>Represents a course optimistic-concurrency conflict.</summary>
public sealed class CourseConcurrencyException : Exception
{
    /// <summary>Initializes the exception.</summary>
    public CourseConcurrencyException(Exception innerException)
        : base("The course was modified by another request.", innerException)
    {
    }
}
