using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Domain.Lessons;
using LearningPortal.Shared.Lessons;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Lessons;

/// <summary>Builds server-derived previews for supported lesson types.</summary>
public sealed class LessonContentPreviewService(IVideoEmbedResolver videos, IMarkdownRenderer markdown)
    : ILessonContentPreviewService
{
    /// <inheritdoc />
    public Result<LessonContentPreviewResponse> Preview(LessonContentPreviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var content = LessonSupport.ResolveContent(request.LessonType, request.MarkdownContent, request.ExternalUrl, videos);
        if (content.IsFailure) return Result<LessonContentPreviewResponse>.Failure(content.Error!);
        var value = content.Value;
        if (value.LessonType == LessonType.Article)
            return Result<LessonContentPreviewResponse>.Success(new(value.LessonType.ToString(),
                markdown.Render(value.MarkdownContent!), null, VideoProvider.None.ToString(), null, false));
        if (value.LessonType == LessonType.Video)
        {
            var video = videos.Resolve(value.ExternalUrl!);
            if (video.IsFailure) return Result<LessonContentPreviewResponse>.Failure(video.Error!);
            return Result<LessonContentPreviewResponse>.Success(new(value.LessonType.ToString(), null,
                video.Value.NormalizedSourceUrl, video.Value.VideoProvider, video.Value.EmbedUrl, video.Value.IsDirectVideo));
        }
        return Result<LessonContentPreviewResponse>.Success(new(value.LessonType.ToString(), null,
            value.ExternalUrl, VideoProvider.None.ToString(), null, false));
    }
}
