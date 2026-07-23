using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Queries.GetUserById;

/// <summary>Delegates a user-by-identifier read to the Identity-neutral application port.</summary>
public sealed class GetUserByIdQueryHandler(IUserManagementService userManagementService)
    : IQueryHandler<GetUserByIdQuery, Result<UserResponse>>
{
    /// <inheritdoc />
    public Task<Result<UserResponse>> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken = default) =>
        userManagementService.GetUserByIdAsync(query.UserId, cancellationToken);
}
