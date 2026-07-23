using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Abstractions.Lessons;

/// <summary>Validates video source URLs and produces trusted playback data.</summary>
public interface IVideoEmbedResolver
{
    /// <summary>Resolves a source URL without performing network access.</summary>
    Result<VideoEmbedResult> Resolve(string sourceUrl);
}

/// <summary>Contains normalized, server-derived video playback data.</summary>
public sealed record VideoEmbedResult(string VideoProvider, string NormalizedSourceUrl, string EmbedUrl, bool IsDirectVideo);
