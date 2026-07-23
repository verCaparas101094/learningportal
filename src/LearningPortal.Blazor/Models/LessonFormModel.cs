using System.ComponentModel.DataAnnotations;
namespace LearningPortal.Blazor.Models;
/// <summary>Contains editable lesson form values.</summary>
public sealed class LessonFormModel
{
    /// <summary>Gets or sets the title.</summary>
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the content.</summary>
    [Required, StringLength(100000)] public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the order.</summary>
    [Range(1, int.MaxValue)] public int Order { get; set; } = 1;
    /// <summary>Gets or sets the estimated duration.</summary>
    [Range(1, int.MaxValue)] public int EstimatedMinutes { get; set; } = 1;
    /// <summary>Gets or sets the lesson type.</summary>
    [Required] public string LessonType { get; set; } = "Article";
    /// <summary>Gets or sets the concurrency value.</summary>
    public string RowVersion { get; set; } = string.Empty;
}
