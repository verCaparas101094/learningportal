#pragma warning disable CS1591
namespace LearningPortal.Shared.Certificates;

public sealed record CertificateResponse(
    Guid Id, Guid EnrollmentId, string CertificateNumber, string VerificationCode,
    string StudentDisplayName, string CourseTitle, string CourseCategory, string? InstructorDisplayName,
    DateTimeOffset CompletedAtUtc, DateTimeOffset IssuedAtUtc, string Status,
    DateTimeOffset? RevokedAtUtc, string? RevocationReason);
public sealed record CertificateListItemResponse(
    Guid Id, Guid EnrollmentId, string CertificateNumber, string VerificationCode,
    string CourseTitle, DateTimeOffset IssuedAtUtc, string Status);
public sealed record CertificateVerificationResponse(
    string CertificateNumber, string StudentDisplayName, string CourseTitle, string CourseCategory,
    string? InstructorDisplayName, DateTimeOffset CompletedAtUtc, DateTimeOffset IssuedAtUtc,
    string Status, DateTimeOffset? RevokedAtUtc);
public sealed record IssueCertificateResponse(CertificateResponse Certificate, bool AlreadyExisted);
public sealed record RevokeCertificateRequest(string Reason);
