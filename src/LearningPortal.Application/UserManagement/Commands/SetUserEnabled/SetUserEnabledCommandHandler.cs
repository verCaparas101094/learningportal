using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Commands.SetUserEnabled;

/// <summary>Delegates enabled-state changes to the Identity-neutral application port.</summary>
public sealed class SetUserEnabledCommandHandler(IUserManagementService userManagementService)
    : ICommandHandler<SetUserEnabledCommand, Result<UserResponse>>
{
    /// <inheritdoc />
    public Task<Result<UserResponse>> HandleAsync(
        SetUserEnabledCommand command,
        CancellationToken cancellationToken = default) =>
        userManagementService.SetEnabledAsync(
            command.UserId,
            command.IsEnabled,
            cancellationToken);
}
