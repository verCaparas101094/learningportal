using LearningPortal.Application.Abstractions.Lessons;
using LearningPortal.Domain.Lessons;
using LearningPortal.Shared.Results;

namespace LearningPortal.Infrastructure.Lessons;

/// <summary>Resolves supported external video URLs using exact host rules.</summary>
public sealed class VideoEmbedResolver : IVideoEmbedResolver
{
    private static readonly HashSet<string> YouTubeHosts = new(StringComparer.OrdinalIgnoreCase)
        { "youtube.com", "www.youtube.com", "youtu.be" };
    private static readonly HashSet<string> VimeoHosts = new(StringComparer.OrdinalIgnoreCase)
        { "vimeo.com", "www.vimeo.com", "player.vimeo.com" };

    /// <inheritdoc />
    public Result<VideoEmbedResult> Resolve(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo))
            return Result<VideoEmbedResult>.Failure(Errors.LessonManagement.InvalidContent("A valid absolute HTTPS video URL is required."));

        if (uri.AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            return Success(VideoProvider.DirectMp4, Normalize(uri), Normalize(uri), true);

        if (YouTubeHosts.Contains(uri.IdnHost))
        {
            var id = GetYouTubeId(uri);
            if (!IsSafeIdentifier(id)) return Unsupported();
            return Success(VideoProvider.YouTube, $"https://www.youtube.com/watch?v={id}",
                $"https://www.youtube-nocookie.com/embed/{id}", false);
        }

        if (VimeoHosts.Contains(uri.IdnHost))
        {
            var id = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (string.IsNullOrWhiteSpace(id) || !id.All(char.IsAsciiDigit)) return Unsupported();
            return Success(VideoProvider.Vimeo, $"https://vimeo.com/{id}", $"https://player.vimeo.com/video/{id}", false);
        }

        if (IsMicrosoftHost(uri.IdnHost))
            return Success(VideoProvider.MicrosoftStream, Normalize(uri), Normalize(uri), false);

        return Unsupported();
    }

    private static string? GetYouTubeId(Uri uri)
    {
        if (uri.IdnHost.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
            return uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase)) return segments[1];
        return System.Web.HttpUtility.ParseQueryString(uri.Query).Get("v");
    }

    private static bool IsSafeIdentifier(string? value) =>
        value is { Length: >= 6 and <= 64 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    private static bool IsMicrosoftHost(string host) =>
        host.Equals("stream.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("web.microsoftstream.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".microsoftstream.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase);
    private static string Normalize(Uri uri) => uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    private static Result<VideoEmbedResult> Success(VideoProvider provider, string source, string embed, bool direct) =>
        Result<VideoEmbedResult>.Success(new(provider.ToString(), source, embed, direct));
    private static Result<VideoEmbedResult> Unsupported() =>
        Result<VideoEmbedResult>.Failure(Errors.LessonManagement.InvalidContent("The video URL uses an unsupported provider or format."));
}
