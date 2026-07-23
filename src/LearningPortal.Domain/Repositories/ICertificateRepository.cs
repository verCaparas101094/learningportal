using LearningPortal.Domain.Certificates;

namespace LearningPortal.Domain.Repositories;

/// <summary>Provides certificate persistence operations.</summary>
public interface ICertificateRepository
{
    /// <summary>Gets a certificate by identifier.</summary>
    Task<Certificate?> GetByIdAsync(Guid id, bool readOnly, CancellationToken cancellationToken = default);
    /// <summary>Gets a certificate by enrollment.</summary>
    Task<Certificate?> GetByEnrollmentAsync(Guid enrollmentId, bool readOnly, CancellationToken cancellationToken = default);
    /// <summary>Gets a certificate by public verification code.</summary>
    Task<Certificate?> GetByVerificationCodeAsync(string code, CancellationToken cancellationToken = default);
    /// <summary>Lists certificates for a student.</summary>
    Task<IReadOnlyList<Certificate>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    /// <summary>Lists certificates for a course.</summary>
    Task<IReadOnlyList<Certificate>> GetByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
    /// <summary>Gets the next concurrency-safe certificate sequence.</summary>
    Task<long> GetNextSequenceAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds a certificate.</summary>
    Task AddAsync(Certificate certificate, CancellationToken cancellationToken = default);
}
