namespace LearningPortal.Domain.Lessons.Exceptions;

/// <summary>Represents a duplicate active lesson order within a course.</summary>
public sealed class DuplicateLessonOrderException(Exception innerException)
    : Exception("The lesson order already exists.", innerException);
