namespace LearningPortal.Domain.Courses.Exceptions;

/// <summary>Represents a database-enforced duplicate course slug.</summary>
public sealed class DuplicateCourseSlugException : Exception
{
    /// <summary>Initializes the exception.</summary>
    public DuplicateCourseSlugException(Exception innerException)
        : base("The course slug already exists.", innerException)
    {
    }
}
