using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Application.Courses;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Lessons.Exceptions;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Lessons;

internal static class LessonSupport
{
    public static Error? Authorize(ICurrentUserService user, Course course) =>
        CourseAuthorization.ValidateManager(user) ?? (CourseAuthorization.CanAccess(user, course) ? null : Errors.Authorization.Forbidden());
    public static LessonResponse ToResponse(this Lesson x, IVideoEmbedResolver videos, IMarkdownRenderer markdown)
    {
        VideoEmbedResult? video = null;
        if (x.LessonType == LessonType.Video && x.ExternalUrl is not null)
        {
            var result = videos.Resolve(x.ExternalUrl);
            if (result.IsSuccess) video = result.Value;
        }
        return new(x.Id, x.CourseId, x.Title, x.Description, x.Order, x.EstimatedMinutes, x.LessonType.ToString(),
            x.MarkdownContent, x.ExternalUrl, x.VideoProvider.ToString(), video?.EmbedUrl, video?.IsDirectVideo ?? false,
            x.LessonType == LessonType.Article && x.MarkdownContent is not null ? markdown.Render(x.MarkdownContent) : null,
            x.Status.ToString(), x.CreatedAtUtc, x.UpdatedAtUtc, Convert.ToBase64String(x.RowVersion));
    }
    public static LessonListItemResponse ToListItem(this Lesson x) => new(x.Id, x.CourseId, x.Title, x.Order,
        x.EstimatedMinutes, x.LessonType.ToString(), x.Status.ToString(), Convert.ToBase64String(x.RowVersion));
    public static async Task<Error?> SaveAsync(IUnitOfWork unit, CancellationToken ct)
    {
        try { await unit.SaveChangesAsync(ct); return null; }
        catch (LessonConcurrencyException) { return Errors.LessonManagement.ConcurrencyConflict(); }
        catch (DuplicateLessonOrderException) { return Errors.LessonManagement.DuplicateOrder(); }
    }
    public static bool TryRowVersion(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(value); return bytes.Length > 0; }
        catch (FormatException) { bytes = []; return false; }
    }

    public static Result<LessonContentValues> ResolveContent(
        string lessonType, string? markdownContent, string? externalUrl, IVideoEmbedResolver videos)
    {
        if (!Enum.TryParse<LessonType>(lessonType, true, out var type) || !Enum.IsDefined(type))
            return Result<LessonContentValues>.Failure(Errors.LessonManagement.InvalidContent("The lesson type is invalid."));
        if (type == LessonType.Article)
        {
            if (string.IsNullOrWhiteSpace(markdownContent) || externalUrl is not null ||
                ContainsEmbedMarkup(markdownContent))
                return Result<LessonContentValues>.Failure(
                    Errors.LessonManagement.InvalidContent("Article Markdown is required and cannot contain embedded content."));
            return Result<LessonContentValues>.Success(new(type, markdownContent, null, VideoProvider.None));
        }
        if (markdownContent is not null)
            return Result<LessonContentValues>.Failure(
                Errors.LessonManagement.InvalidContent("Markdown content is only supported for Article lessons."));
        if (type == LessonType.Video)
        {
            if (string.IsNullOrWhiteSpace(externalUrl))
                return Result<LessonContentValues>.Failure(Errors.LessonManagement.InvalidContent("A video URL is required."));
            var video = videos.Resolve(externalUrl);
            return video.IsFailure
                ? Result<LessonContentValues>.Failure(video.Error!)
                : Result<LessonContentValues>.Success(new(type, null, video.Value.NormalizedSourceUrl,
                    Enum.Parse<VideoProvider>(video.Value.VideoProvider)));
        }
        if (!IsAbsoluteHttps(externalUrl))
            return Result<LessonContentValues>.Failure(Errors.LessonManagement.InvalidContent("A valid absolute HTTPS URL is required."));
        return Result<LessonContentValues>.Success(new(type, null, NormalizeUrl(externalUrl!), VideoProvider.None));
    }

    private static bool IsAbsoluteHttps(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo);
    private static string NormalizeUrl(string value) =>
        new Uri(value).GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    private static bool ContainsEmbedMarkup(string value) =>
        value.Contains("<iframe", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("<embed", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("<object", StringComparison.OrdinalIgnoreCase);
}

internal sealed record LessonContentValues(
    LessonType LessonType,
    string? MarkdownContent,
    string? ExternalUrl,
    VideoProvider VideoProvider);
