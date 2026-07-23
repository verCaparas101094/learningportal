using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Certificates;

/// <summary>Represents an immutable course-completion credential snapshot.</summary>
public sealed class Certificate : AuditableEntity
{
    private Certificate() { }
    private Certificate(string number, Guid enrollmentId, Guid courseId, Guid studentId,
        string studentName, string courseTitle, string category, string? instructorName,
        DateTimeOffset completedAtUtc, DateTimeOffset issuedAtUtc, string verificationCode)
    {
        CertificateNumber = number; EnrollmentId = enrollmentId; CourseId = courseId; StudentId = studentId;
        StudentDisplayName = studentName; CourseTitle = courseTitle; CourseCategory = category;
        InstructorDisplayName = instructorName; CompletedAtUtc = completedAtUtc; IssuedAtUtc = issuedAtUtc;
        VerificationCode = verificationCode; Status = CertificateStatus.Active;
    }

    /// <summary>Gets the public certificate number.</summary>
    public string CertificateNumber { get; private set; } = string.Empty;
    /// <summary>Gets the completed enrollment.</summary>
    public Guid EnrollmentId { get; private set; }
    /// <summary>Gets the snapshotted course identifier.</summary>
    public Guid CourseId { get; private set; }
    /// <summary>Gets the credential owner.</summary>
    public Guid StudentId { get; private set; }
    /// <summary>Gets the snapshotted student name.</summary>
    public string StudentDisplayName { get; private set; } = string.Empty;
    /// <summary>Gets the snapshotted course title.</summary>
    public string CourseTitle { get; private set; } = string.Empty;
    /// <summary>Gets the snapshotted category or skill.</summary>
    public string CourseCategory { get; private set; } = string.Empty;
    /// <summary>Gets the optional snapshotted instructor name.</summary>
    public string? InstructorDisplayName { get; private set; }
    /// <summary>Gets the course completion time.</summary>
    public DateTimeOffset CompletedAtUtc { get; private set; }
    /// <summary>Gets the issue time.</summary>
    public DateTimeOffset IssuedAtUtc { get; private set; }
    /// <summary>Gets the public random verification code.</summary>
    public string VerificationCode { get; private set; } = string.Empty;
    /// <summary>Gets certificate status.</summary>
    public CertificateStatus Status { get; private set; }
    /// <summary>Gets revocation time.</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    /// <summary>Gets the administrative revocation reason.</summary>
    public string? RevocationReason { get; private set; }

    /// <summary>Creates an active certificate from trusted snapshots.</summary>
    public static Certificate Issue(string number, Guid enrollmentId, Guid courseId, Guid studentId,
        string studentName, string courseTitle, string category, string? instructorName,
        DateTimeOffset completedAtUtc, DateTimeOffset issuedAtUtc, string verificationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number); ArgumentException.ThrowIfNullOrWhiteSpace(verificationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(studentName); ArgumentException.ThrowIfNullOrWhiteSpace(courseTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (enrollmentId == Guid.Empty || courseId == Guid.Empty || studentId == Guid.Empty
            || completedAtUtc.Offset != TimeSpan.Zero || issuedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Certificate values are invalid.");
        return new(number.Trim(), enrollmentId, courseId, studentId, studentName.Trim(), courseTitle.Trim(),
            category.Trim(), string.IsNullOrWhiteSpace(instructorName) ? null : instructorName.Trim(),
            completedAtUtc, issuedAtUtc, verificationCode.Trim());
    }

    /// <summary>Revokes an active certificate and treats repeat requests as success.</summary>
    public bool TryRevoke(string reason, DateTimeOffset revokedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000 || revokedAtUtc.Offset != TimeSpan.Zero) return false;
        if (Status == CertificateStatus.Revoked) return true;
        Status = CertificateStatus.Revoked; RevokedAtUtc = revokedAtUtc; RevocationReason = reason.Trim(); return true;
    }
}
