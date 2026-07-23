namespace LearningPortal.Shared.Lessons;

/// <summary>Contains lesson search and paging values.</summary>
public sealed class GetLessonsRequest
{
    /// <summary>Gets or sets the search term.</summary>
    public string? Search { get; set; }
    /// <summary>Gets or sets the one-based page number.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; } = 10;
}
