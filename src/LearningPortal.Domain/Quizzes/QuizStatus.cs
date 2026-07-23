namespace LearningPortal.Domain.Quizzes;

/// <summary>Defines the quiz lifecycle.</summary>
public enum QuizStatus
{
    /// <summary>Quiz is editable and unavailable to learners.</summary>
    Draft,
    /// <summary>Quiz is available to eligible learners.</summary>
    Published,
    /// <summary>Quiz is retained but unavailable for new attempts.</summary>
    Archived
}
