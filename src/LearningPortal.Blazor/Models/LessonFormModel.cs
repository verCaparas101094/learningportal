using System.ComponentModel.DataAnnotations;
namespace LearningPortal.Blazor.Models;
/// <summary>Contains editable lesson form values.</summary>
public sealed class LessonFormModel : IValidatableObject
{
    /// <summary>Gets or sets the title.</summary>
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets article Markdown source.</summary>
    [StringLength(100000)] public string? MarkdownContent { get; set; }
    /// <summary>Gets or sets an external content URL.</summary>
    [Url, StringLength(2048)] public string? ExternalUrl { get; set; }
    /// <summary>Gets or sets the order.</summary>
    [Range(1, int.MaxValue)] public int Order { get; set; } = 1;
    /// <summary>Gets or sets the estimated duration.</summary>
    [Range(1, int.MaxValue)] public int EstimatedMinutes { get; set; } = 1;
    /// <summary>Gets or sets the lesson type.</summary>
    [Required] public string LessonType { get; set; } = "Article";
    /// <summary>Gets or sets the concurrency value.</summary>
    public string RowVersion { get; set; } = string.Empty;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LessonType == "Article")
        {
            if (string.IsNullOrWhiteSpace(MarkdownContent))
                yield return new("Markdown content is required.", [nameof(MarkdownContent)]);
            yield break;
        }
        if (string.IsNullOrWhiteSpace(ExternalUrl) ||
            !Uri.TryCreate(ExternalUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
            yield return new("An absolute HTTPS URL is required.", [nameof(ExternalUrl)]);
    }
}
