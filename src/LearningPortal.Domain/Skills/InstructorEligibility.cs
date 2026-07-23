using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Skills;

/// <summary>Records a user's highest earned qualification for one skill.</summary>
public sealed class InstructorEligibility : AuditableEntity
{
    private InstructorEligibility() { }
    private InstructorEligibility(
        Guid userId, Guid skillId, Guid qualifyingQuizId, decimal percentage, DateTimeOffset qualifiedAtUtc)
    {
        UserId = userId;
        SkillId = skillId;
        QualifyingQuizId = qualifyingQuizId;
        BestPercentage = percentage;
        QualifiedAtUtc = qualifiedAtUtc;
        IsEligible = true;
    }

    /// <summary>Gets the qualified user.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Gets the qualified skill.</summary>
    public Guid SkillId { get; private set; }
    /// <summary>Gets the quiz that produced the best score.</summary>
    public Guid QualifyingQuizId { get; private set; }
    /// <summary>Gets the highest qualifying percentage.</summary>
    public decimal BestPercentage { get; private set; }
    /// <summary>Gets when qualification was first earned.</summary>
    public DateTimeOffset QualifiedAtUtc { get; private set; }
    /// <summary>Gets whether the qualification remains active.</summary>
    public bool IsEligible { get; private set; }

    /// <summary>Creates a qualification from a qualifying submitted result.</summary>
    public static InstructorEligibility Create(
        Guid userId, Guid skillId, Guid qualifyingQuizId, decimal percentage, DateTimeOffset qualifiedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(skillId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(qualifyingQuizId, Guid.Empty);
        if (percentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percentage));
        if (qualifiedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must use UTC.", nameof(qualifiedAtUtc));
        return new(userId, skillId, qualifyingQuizId, percentage, qualifiedAtUtc);
    }

    /// <summary>Updates only when a higher qualifying result is supplied.</summary>
    public bool TryUpdateBest(Guid qualifyingQuizId, decimal percentage)
    {
        if (!IsEligible || qualifyingQuizId == Guid.Empty || percentage <= BestPercentage || percentage > 100)
            return false;
        QualifyingQuizId = qualifyingQuizId;
        BestPercentage = percentage;
        return true;
    }
}
