using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Quizzes;

/// <summary>Represents one ordered, automatically scored quiz question.</summary>
public sealed class QuizQuestion : AuditableEntity
{
    private QuizQuestion() { }
    private QuizQuestion(Guid quizId, string text, QuestionType type, decimal points, int order, string? explanation)
    { QuizId = quizId; Text = text; QuestionType = type; Points = points; Order = order; Explanation = explanation; }
    /// <summary>Gets the owning quiz.</summary>
    public Guid QuizId { get; private set; }
    /// <summary>Gets the question text.</summary>
    public string Text { get; private set; } = string.Empty;
    /// <summary>Gets the scoring type.</summary>
    public QuestionType QuestionType { get; private set; }
    /// <summary>Gets the available score.</summary>
    public decimal Points { get; private set; }
    /// <summary>Gets stable display order.</summary>
    public int Order { get; private set; }
    /// <summary>Gets optional learner feedback.</summary>
    public string? Explanation { get; private set; }
    /// <summary>Gets whether the question appears in new attempts.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Creates a valid active question.</summary>
    public static QuizQuestion Create(Guid quizId, string text, QuestionType questionType, decimal points, int order, string? explanation = null)
    { if (quizId == Guid.Empty || string.IsNullOrWhiteSpace(text) || points <= 0 || order < 1) throw new ArgumentException("Question values are invalid."); return new(quizId, text.Trim(), questionType, points, order, string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim()); }
    /// <summary>Updates editable question details.</summary>
    public bool TryUpdate(string text, QuestionType questionType, decimal points, string? explanation)
    { if (string.IsNullOrWhiteSpace(text) || points <= 0) return false; Text = text.Trim(); QuestionType = questionType; Points = points; Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim(); return true; }
    /// <summary>Activates the question.</summary>
    public void Activate() => IsActive = true;
    /// <summary>Deactivates the question.</summary>
    public void Deactivate() => IsActive = false;
    /// <summary>Changes display order.</summary>
    public bool TryReorder(int order) { if (order < 1) return false; Order = order; return true; }
    /// <summary>Validates choices for the configured type.</summary>
    public bool HasValidAnswers(IReadOnlyCollection<QuizAnswerChoice> choices)
    { var correct = choices.Count(x => x.IsCorrect); return QuestionType switch { QuestionType.SingleChoice => choices.Count > 0 && correct == 1, QuestionType.MultipleChoice => choices.Count > 0 && correct >= 1, QuestionType.TrueFalse => choices.Count == 2 && correct == 1, _ => false }; }
}
