using LearningPortal.Domain.Common;

namespace LearningPortal.Domain.Quizzes;

/// <summary>Represents a course assessment authored before publication.</summary>
public sealed class Quiz : AuditableEntity
{
    private Quiz() { }
    private Quiz(Guid courseId, Guid? lessonId, string title, string description, decimal passingPercentage, int? maximumAttempts, bool required)
    { CourseId = courseId; LessonId = lessonId; Title = title; Description = description; PassingPercentage = passingPercentage; MaximumAttempts = maximumAttempts; IsRequiredForCourseCompletion = required; }
    /// <summary>Gets the owning course.</summary>
    public Guid CourseId { get; private set; }
    /// <summary>Gets the optional associated lesson.</summary>
    public Guid? LessonId { get; private set; }
    /// <summary>Gets the quiz title.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>Gets the learner instructions.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Gets the passing threshold.</summary>
    public decimal PassingPercentage { get; private set; }
    /// <summary>Gets the optional attempt limit.</summary>
    public int? MaximumAttempts { get; private set; }
    /// <summary>Gets whether passing blocks course completion.</summary>
    public bool IsRequiredForCourseCompletion { get; private set; }
    /// <summary>Gets the lifecycle state.</summary>
    public QuizStatus Status { get; private set; } = QuizStatus.Draft;
    /// <summary>Creates a draft quiz.</summary>
    public static Quiz Create(Guid courseId, Guid? lessonId, string title, string description, decimal passingPercentage, int? maximumAttempts, bool required)
    {
        if (courseId == Guid.Empty || string.IsNullOrWhiteSpace(title) || passingPercentage is < 1 or > 100 || maximumAttempts is <= 0)
            throw new ArgumentException("Quiz values are invalid.");
        return new(courseId, lessonId, title.Trim(), description?.Trim() ?? string.Empty, passingPercentage, maximumAttempts, required);
    }
    /// <summary>Updates a draft quiz's details.</summary>
    public bool TryUpdate(string title, string description, decimal passingPercentage, int? maximumAttempts, bool required)
    {
        if (Status != QuizStatus.Draft || string.IsNullOrWhiteSpace(title) || passingPercentage is < 1 or > 100 || maximumAttempts is <= 0) return false;
        Title = title.Trim(); Description = description?.Trim() ?? string.Empty; PassingPercentage = passingPercentage; MaximumAttempts = maximumAttempts; IsRequiredForCourseCompletion = required; return true;
    }
    /// <summary>Publishes a valid draft quiz.</summary>
    public bool TryPublish(int validQuestionCount) { if (Status == QuizStatus.Published) return true; if (Status != QuizStatus.Draft || validQuestionCount < 1) return false; Status = QuizStatus.Published; return true; }
    /// <summary>Archives a published quiz.</summary>
    public bool TryArchive() { if (Status == QuizStatus.Archived) return true; if (Status != QuizStatus.Published) return false; Status = QuizStatus.Archived; return true; }
}
