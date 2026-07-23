using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Quizzes;

/// <summary>Preserves one submitted answer and its server scoring snapshot.</summary>
public sealed class QuizAttemptAnswer : Entity
{
    private QuizAttemptAnswer()
    {
    }

    private QuizAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        string questionText,
        QuestionType questionType,
        string selectedChoiceIds,
        string choiceSnapshot,
        bool isCorrect,
        decimal pointsAwarded,
        decimal maximumPoints,
        string? explanation)
    {
        AttemptId = attemptId;
        QuestionId = questionId;
        QuestionText = questionText;
        QuestionType = questionType;
        SelectedChoiceIds = selectedChoiceIds;
        ChoiceSnapshot = choiceSnapshot;
        IsCorrect = isCorrect;
        PointsAwarded = pointsAwarded;
        MaximumPoints = maximumPoints;
        Explanation = explanation;
    }

    /// <summary>Gets the owning attempt identifier.</summary>
    public Guid AttemptId { get; private set; }

    /// <summary>Gets the source question identifier.</summary>
    public Guid QuestionId { get; private set; }

    /// <summary>Gets the submitted question text snapshot.</summary>
    public string QuestionText { get; private set; } = string.Empty;

    /// <summary>Gets the submitted question type snapshot.</summary>
    public QuestionType QuestionType { get; private set; }

    /// <summary>Gets the selected answer identifiers as JSON.</summary>
    public string SelectedChoiceIds { get; private set; } = "[]";

    /// <summary>Gets the answer-choice snapshot as JSON.</summary>
    public string ChoiceSnapshot { get; private set; } = "[]";

    /// <summary>Gets whether the exact answer was correct.</summary>
    public bool IsCorrect { get; private set; }

    /// <summary>Gets awarded points.</summary>
    public decimal PointsAwarded { get; private set; }

    /// <summary>Gets available points.</summary>
    public decimal MaximumPoints { get; private set; }

    /// <summary>Gets the feedback snapshot.</summary>
    public string? Explanation { get; private set; }

    /// <summary>Creates a server-scored immutable answer snapshot.</summary>
    public static QuizAttemptAnswer Create(Guid attemptId, QuizQuestion question, IReadOnlyCollection<Guid> selectedChoiceIds)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(attemptId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(selectedChoiceIds);

        var available = question.AnswerChoices.Select(choice => choice.Id).ToHashSet();
        var selected = selectedChoiceIds.Distinct().Order().ToArray();
        if (selected.Any(choiceId => !available.Contains(choiceId)))
        {
            throw new ArgumentException("A selected answer choice does not belong to the question.", nameof(selectedChoiceIds));
        }

        var correct = question.AnswerChoices
            .Where(choice => choice.IsCorrect)
            .Select(choice => choice.Id)
            .Order()
            .ToArray();
        var isCorrect = selected.SequenceEqual(correct);
        return new QuizAttemptAnswer(
            attemptId,
            question.Id,
            question.Text,
            question.QuestionType,
            System.Text.Json.JsonSerializer.Serialize(selected),
            System.Text.Json.JsonSerializer.Serialize(question.AnswerChoices.OrderBy(x => x.Order)
                .Select(x => new { x.Id, x.Text, x.Order, x.IsCorrect })),
            isCorrect,
            isCorrect ? question.Points : 0m,
            question.Points,
            question.Explanation);
    }
}
