#pragma warning disable CS1591
namespace LearningPortal.Shared.InstructorEligibility;

public sealed record SkillResponse(Guid Id, string Name, string Slug, string? Description, bool IsActive);
public sealed record InstructorEligibilityResponse(
    Guid UserId,
    Guid SkillId,
    string SkillName,
    bool IsEligible,
    decimal BestPercentage,
    DateTimeOffset QualifiedAtUtc,
    Guid QualifyingQuizId);
public sealed record EligibleInstructorResponse(
    Guid UserId, string DisplayName, Guid SkillId, decimal BestPercentage, DateTimeOffset QualifiedAtUtc);
public sealed record AssignCourseInstructorRequest(Guid InstructorId, string? RowVersion = null);
