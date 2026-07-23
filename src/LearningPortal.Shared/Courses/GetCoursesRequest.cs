namespace LearningPortal.Shared.Courses;

/// <summary>Models course search, status, and pagination values.</summary>
public sealed class GetCoursesRequest
{
    /// <summary>Gets the optional title, slug, or category search term.</summary>
    public string? Search { get; init; }

    /// <summary>Gets the optional Draft, Published, or Archived status.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Gets the page size.</summary>
    public int PageSize { get; init; } = 10;
}
