using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Learning;
using LearningPortal.Domain.Lessons;
using LearningPortal.Shared.Learning;

namespace LearningPortal.Application.Learning;

internal static class LearningMappings
{
    public static LessonProgressResponse ToResponse(this LessonProgress progress) => new(progress.LessonId, progress.Status.ToString(), progress.StartedAtUtc, progress.CompletedAtUtc, progress.LastAccessedAtUtc, Convert.ToBase64String(progress.RowVersion));
    public static CourseProgressResponse ToCourseProgress(Enrollment enrollment, IReadOnlyList<Lesson> lessons, IReadOnlyList<LessonProgress> progress) => new(enrollment.Id, enrollment.Status.ToString(), progress.Count(x => x.Status == LessonProgressStatus.Completed), lessons.Count, lessons.Count == 0 ? 0 : progress.Count(x => x.Status == LessonProgressStatus.Completed) * 100 / lessons.Count, enrollment.Status == EnrollmentStatus.Completed);
    public static (string? Html, string? Source, string? Embed, bool Direct) Content(Lesson lesson, IMarkdownRenderer markdown, IVideoEmbedResolver videos)
    {
        if (lesson.LessonType == LessonType.Article) return (markdown.Render(lesson.MarkdownContent!), null, null, false);
        if (lesson.LessonType == LessonType.Video)
        {
            var video = videos.Resolve(lesson.ExternalUrl!);
            return video.IsSuccess ? (null, video.Value.NormalizedSourceUrl, video.Value.EmbedUrl, video.Value.IsDirectVideo) : (null, null, null, false);
        }
        return (null, lesson.ExternalUrl, null, false);
    }
}
