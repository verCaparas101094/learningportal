namespace LearningPortal.Domain.Certificates;

/// <summary>Defines certificate lifecycle state.</summary>
public enum CertificateStatus
{
    /// <summary>The certificate is valid.</summary>
    Active,
    /// <summary>The certificate remains historical but is no longer valid.</summary>
    Revoked
}
