namespace LearningPortal.Shared.UserManagement;

/// <summary>
/// Models user search and pagination query-string values.
/// </summary>
public sealed class GetUsersRequest
{
    /// <summary>Gets an optional email or display-name search term.</summary>
    public string? Search { get; init; }

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Gets the number of users requested per page.</summary>
    public int PageSize { get; init; } = 20;
}
