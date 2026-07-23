#pragma warning disable CS1591
using LearningPortal.Domain.Common;
namespace LearningPortal.Domain.Quizzes;
public sealed class Quiz : AuditableEntity
{
    private Quiz() { }
    private Quiz(Guid courseId, Guid? lessonId, string title, string description, decimal passingPercentage, int? maximumAttempts, bool required) { CourseId=courseId; LessonId=lessonId; Title=title; Description=description; PassingPercentage=passingPercentage; MaximumAttempts=maximumAttempts; IsRequiredForCourseCompletion=required; }
    public Guid CourseId { get; private set; } public Guid? LessonId { get; private set; } public string Title { get; private set; } = string.Empty; public string Description { get; private set; } = string.Empty; public decimal PassingPercentage { get; private set; } public int? MaximumAttempts { get; private set; } public bool IsRequiredForCourseCompletion { get; private set; } public QuizStatus Status { get; private set; } = QuizStatus.Draft;
    public static Quiz Create(Guid courseId, Guid? lessonId, string title, string description, decimal passingPercentage, int? maximumAttempts, bool required)
    { if(courseId==Guid.Empty || string.IsNullOrWhiteSpace(title) || passingPercentage is < 1 or > 100 || maximumAttempts is <= 0) throw new ArgumentException("Quiz values are invalid."); return new(courseId,lessonId,title.Trim(),description?.Trim()??string.Empty,passingPercentage,maximumAttempts,required); }
    public bool TryPublish(int validQuestionCount) { if(Status==QuizStatus.Published)return true; if(Status!=QuizStatus.Draft || validQuestionCount<1)return false; Status=QuizStatus.Published; return true; }
    public bool TryArchive() { if(Status==QuizStatus.Archived)return true; if(Status!=QuizStatus.Published)return false; Status=QuizStatus.Archived; return true; }
}
