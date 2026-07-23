using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Quizzes;

/// <summary>Represents one answer choice authored for a quiz question.</summary>
public sealed class QuizAnswerChoice : Entity
{
    private QuizAnswerChoice() { }
    private QuizAnswerChoice(Guid questionId, string text, bool isCorrect, int order) { QuestionId = questionId; Text = text; IsCorrect = isCorrect; Order = order; }
    /// <summary>Gets the owning question.</summary>
    public Guid QuestionId { get; private set; }
    /// <summary>Gets the choice text.</summary>
    public string Text { get; private set; } = string.Empty;
    /// <summary>Gets whether the choice is correct.</summary>
    public bool IsCorrect { get; private set; }
    /// <summary>Gets display order.</summary>
    public int Order { get; private set; }
    /// <summary>Creates a validated answer choice.</summary>
    public static QuizAnswerChoice Create(Guid questionId, string text, bool isCorrect, int order) { ArgumentOutOfRangeException.ThrowIfEqual(questionId, Guid.Empty); if (string.IsNullOrWhiteSpace(text) || order < 1) throw new ArgumentException("Choice text and order are required."); return new(questionId, text.Trim(), isCorrect, order); }
    /// <summary>Updates editable choice values.</summary>
    public bool TryUpdate(string text, bool isCorrect) { if (string.IsNullOrWhiteSpace(text)) return false; Text = text.Trim(); IsCorrect = isCorrect; return true; }
    /// <summary>Changes display order.</summary>
    public bool TryReorder(int order) { if (order < 1) return false; Order = order; return true; }
}
