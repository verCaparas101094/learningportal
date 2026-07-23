using System.ComponentModel.DataAnnotations;
using LearningPortal.Shared.Courses;

namespace LearningPortal.Blazor.Models;

/// <summary>Contains validated course form values.</summary>
public sealed class CourseFormModel
{
    /// <summary>Gets or sets the title.</summary>
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the slug candidate.</summary>
    [Required, StringLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    [StringLength(5_000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the category.</summary>
    [Required, StringLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional thumbnail URL.</summary>
    [StringLength(2_048), Url]
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the administrator-selected instructor.</summary>
    public Guid? InstructorId { get; set; }

    /// <summary>Gets or sets the concurrency value for editing.</summary>
    public string RowVersion { get; set; } = string.Empty;

    /// <summary>Creates form values from a course response.</summary>
    public static CourseFormModel FromCourse(CourseResponse course) => new()
    {
        Title = course.Title,
        Slug = course.Slug,
        Description = course.Description,
        Category = course.Category,
        ThumbnailUrl = course.ThumbnailUrl,
        InstructorId = course.InstructorId,
        RowVersion = course.RowVersion
    };
}
