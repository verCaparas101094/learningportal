namespace LearningPortal.Domain.Certificates;

/// <summary>Represents a concurrent attempt to issue a second certificate for one enrollment.</summary>
public sealed class DuplicateCertificateEnrollmentException(Exception innerException)
    : Exception("A certificate already exists for the enrollment.", innerException);
