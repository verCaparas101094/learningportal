using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Lessons;

/// <summary>Builds safe, non-persisted lesson content previews.</summary>
public interface ILessonContentPreviewService
{
    /// <summary>Validates source content and returns safe preview data.</summary>
    Result<LessonContentPreviewResponse> Preview(LessonContentPreviewRequest request);
}
