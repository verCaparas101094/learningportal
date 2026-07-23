namespace LearningPortal.Application.AiTutor;
/// <summary>Centralizes authorization and privacy-safe course context construction.</summary>
public interface IAiTutorContextBuilder
{
    /// <summary>Validates learner scope and builds bounded learner-visible context.</summary>
    Task<AiTutorContextResult> BuildAsync(Guid studentId,Guid courseId,Guid? lessonId,CancellationToken cancellationToken);
}
/// <summary>Represents safe context construction.</summary>
public sealed record AiTutorContextResult(bool Success,string? Context,string? CourseTitle,string? LessonTitle,LearningPortal.Shared.Results.Error? Error);
