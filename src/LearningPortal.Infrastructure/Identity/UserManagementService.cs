using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Authorization;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Implements compact administrator user management through ASP.NET Identity.
/// </summary>
public sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserManagementService> logger)
    : IUserManagementService
{
    /// <inheritdoc />
    public async Task<Result<PagedUsersResponse>> GetUsersAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100)
        {
            return Result<PagedUsersResponse>.Failure(
                Errors.Validation.Failed("Page number must be at least 1 and page size must be between 1 and 100."));
        }

        var query = userManager.Users.AsNoTracking();
        var normalizedSearch = search?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                (user.Email != null && user.Email.Contains(normalizedSearch))
                || user.DisplayName.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        var users = await query
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = new List<UserResponse>(users.Count);

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(await CreateResponseAsync(user));
        }

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
        return Result<PagedUsersResponse>.Success(new PagedUsersResponse(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages));
    }

    /// <inheritdoc />
    public async Task<Result<UserResponse>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? Result<UserResponse>.Failure(Errors.UserManagement.UserNotFound(userId))
            : Result<UserResponse>.Success(await CreateResponseAsync(user));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, UserResponse>> GetUsersByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync(cancellationToken);
        var responses = new Dictionary<Guid, UserResponse>(users.Count);
        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            responses[user.Id] = await CreateResponseAsync(user);
        }
        return responses;
    }

    /// <inheritdoc />
    public async Task<Result<UserResponse>> SetEnabledAsync(
        Guid userId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserResponse>.Failure(Errors.UserManagement.UserNotFound(userId));
        }

        if (user.IsEnabled != isEnabled)
        {
            user.IsEnabled = isEnabled;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogWarning(
                    "Failed to change enabled state for user {UserId}. Identity errors: {ErrorCodes}.",
                    userId,
                    updateResult.Errors.Select(error => error.Code).ToArray());
                return Result<UserResponse>.Failure(Errors.UserManagement.UpdateFailed());
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<UserResponse>.Success(await CreateResponseAsync(user));
    }

    /// <inheritdoc />
    public async Task<Result<UserResponse>> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!ApplicationRoles.IsValid(role))
        {
            return Result<UserResponse>.Failure(Errors.UserManagement.InvalidRole());
        }

        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserResponse>.Failure(Errors.UserManagement.UserNotFound(userId));
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var assignmentResult = await userManager.AddToRoleAsync(user, role);
            if (!assignmentResult.Succeeded)
            {
                logger.LogWarning(
                    "Failed to assign role {Role} to user {UserId}. Identity errors: {ErrorCodes}.",
                    role,
                    userId,
                    assignmentResult.Errors.Select(error => error.Code).ToArray());
                return Result<UserResponse>.Failure(Errors.UserManagement.RoleAssignmentFailed());
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<UserResponse>.Success(await CreateResponseAsync(user));
    }

    private async Task<UserResponse> CreateResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsEnabled,
            roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
