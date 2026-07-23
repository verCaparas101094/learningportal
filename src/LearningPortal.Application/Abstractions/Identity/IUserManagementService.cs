using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.Abstractions.Identity;

/// <summary>
/// Defines administrator user-management operations without exposing ASP.NET Identity types.
/// </summary>
public interface IUserManagementService
{
    /// <summary>Returns one filtered and paginated user page.</summary>
    Task<Result<PagedUsersResponse>> GetUsersAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Returns one user by identifier.</summary>
    Task<Result<UserResponse>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Enables or disables one user.</summary>
    Task<Result<UserResponse>> SetEnabledAsync(
        Guid userId,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    /// <summary>Adds one valid application role without removing existing roles.</summary>
    Task<Result<UserResponse>> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);
}
