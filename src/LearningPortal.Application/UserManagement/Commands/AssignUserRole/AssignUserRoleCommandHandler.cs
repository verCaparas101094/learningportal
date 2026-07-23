using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Commands.AssignUserRole;

/// <summary>Delegates additive role assignments to the Identity-neutral application port.</summary>
public sealed class AssignUserRoleCommandHandler(IUserManagementService userManagementService)
    : ICommandHandler<AssignUserRoleCommand, Result<UserResponse>>
{
    /// <inheritdoc />
    public Task<Result<UserResponse>> HandleAsync(
        AssignUserRoleCommand command,
        CancellationToken cancellationToken = default) =>
        userManagementService.AssignRoleAsync(
            command.UserId,
            command.Role,
            cancellationToken);
}
