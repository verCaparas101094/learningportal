namespace LearningPortal.Shared.UserManagement;

/// <summary>
/// Contains one page of administrator-safe user projections.
/// </summary>
/// <param name="Items">The users in the requested page.</param>
/// <param name="PageNumber">The one-based page number.</param>
/// <param name="PageSize">The requested page size.</param>
/// <param name="TotalCount">The total matching user count.</param>
/// <param name="TotalPages">The total number of available pages.</param>
public sealed record PagedUsersResponse(
    IReadOnlyCollection<UserResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
