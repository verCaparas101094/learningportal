using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Quizzes;

/// <summary>Represents one owned, server-scored quiz attempt.</summary>
public sealed class QuizAttempt : AuditableEntity
{
    private readonly List<QuizAttemptAnswer> _answers = [];
    private QuizAttempt()
    {
    }

    private QuizAttempt(Guid quizId, Guid enrollmentId, Guid studentId, int attemptNumber, DateTimeOffset startedAtUtc)
    {
        QuizId = quizId;
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        AttemptNumber = attemptNumber;
        StartedAtUtc = startedAtUtc;
        Status = QuizAttemptStatus.InProgress;
    }
    /// <summary>Gets the quiz.</summary>
    public Guid QuizId { get; private set; }
    /// <summary>Gets the enrollment.</summary>
    public Guid EnrollmentId { get; private set; }
    /// <summary>Gets the learner.</summary>
    public Guid StudentId { get; private set; }
    /// <summary>Gets the one-based attempt number.</summary>
    public int AttemptNumber { get; private set; }
    /// <summary>Gets lifecycle state.</summary>
    public QuizAttemptStatus Status { get; private set; }
    /// <summary>Gets start time.</summary>
    public DateTimeOffset StartedAtUtc { get; private set; }
    /// <summary>Gets submission time.</summary>
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    /// <summary>Gets awarded points.</summary>
    public decimal Score { get; private set; }
    /// <summary>Gets available points.</summary>
    public decimal MaximumScore { get; private set; }
    /// <summary>Gets percentage score.</summary>
    public decimal Percentage { get; private set; }
    /// <summary>Gets whether the passing threshold was met.</summary>
    public bool Passed { get; private set; }
    /// <summary>Gets immutable submitted answer snapshots.</summary>
    public IReadOnlyCollection<QuizAttemptAnswer> Answers => _answers;

    /// <summary>Starts an owned attempt.</summary>
    public static QuizAttempt Start(Guid quizId, Guid enrollmentId, Guid studentId, int attemptNumber, DateTimeOffset startedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(quizId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(enrollmentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(attemptNumber, 1);
        EnsureUtc(startedAtUtc);
        return new QuizAttempt(quizId, enrollmentId, studentId, attemptNumber, startedAtUtc);
    }
    /// <summary>Finalizes the attempt idempotently.</summary>
    public bool TrySubmit(IEnumerable<QuizAttemptAnswer> answers, decimal passingPercentage, DateTimeOffset submittedAtUtc)
    {
        if (Status == QuizAttemptStatus.Submitted)
        {
            return false;
        }

        EnsureUtc(submittedAtUtc);
        var snapshots = answers.ToArray();
        if (snapshots.Length == 0
            || snapshots.Any(answer => answer.AttemptId != Id)
            || snapshots.Select(answer => answer.QuestionId).Distinct().Count() != snapshots.Length)
        {
            return false;
        }

        MaximumScore = snapshots.Sum(answer => answer.MaximumPoints);
        if (MaximumScore <= 0)
        {
            return false;
        }

        _answers.AddRange(snapshots);
        Score = snapshots.Sum(answer => answer.PointsAwarded);
        Percentage = decimal.Round(Score * 100m / MaximumScore, 2, MidpointRounding.AwayFromZero);
        Passed = Percentage >= passingPercentage;
        SubmittedAtUtc = submittedAtUtc;
        Status = QuizAttemptStatus.Submitted;
        return true;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The timestamp must use UTC.", nameof(value));
        }
    }
}
