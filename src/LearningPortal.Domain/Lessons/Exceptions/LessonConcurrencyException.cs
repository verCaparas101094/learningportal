namespace LearningPortal.Domain.Lessons.Exceptions;

/// <summary>Represents an optimistic-concurrency conflict for a lesson.</summary>
public sealed class LessonConcurrencyException(Exception innerException)
    : Exception("The lesson was concurrently modified.", innerException);
