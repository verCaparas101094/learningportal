namespace LearningPortal.Domain.Enrollments.Exceptions;

/// <summary>Represents a duplicate active enrollment constraint violation.</summary>
public sealed class DuplicateActiveEnrollmentException(Exception innerException)
    : Exception("An active enrollment already exists.", innerException);
