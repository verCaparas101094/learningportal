namespace LearningPortal.Domain.Quizzes;

/// <summary>Defines the quiz-attempt lifecycle.</summary>
public enum QuizAttemptStatus
{
    /// <summary>The learner may still submit answers.</summary>
    InProgress,
    /// <summary>The server scored and finalized the attempt.</summary>
    Submitted
}
