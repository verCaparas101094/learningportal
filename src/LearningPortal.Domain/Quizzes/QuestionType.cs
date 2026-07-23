namespace LearningPortal.Domain.Quizzes;

/// <summary>Defines supported automatically scored quiz question types.</summary>
public enum QuestionType
{
    /// <summary>Requires one selected answer.</summary>
    SingleChoice,
    /// <summary>Requires an exact selected answer set.</summary>
    MultipleChoice,
    /// <summary>Requires one of two true/false answers.</summary>
    TrueFalse
}
