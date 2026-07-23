namespace LearningPortal.Domain.Enrollments.Exceptions;

/// <summary>Represents an optimistic enrollment concurrency conflict.</summary>
public sealed class EnrollmentConcurrencyException(Exception innerException)
    : Exception("The enrollment changed concurrently.", innerException);
