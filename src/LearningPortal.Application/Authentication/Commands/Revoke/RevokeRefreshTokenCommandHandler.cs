using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Revoke;

/// <summary>
/// Delegates refresh-token revocation to the identity abstraction.
/// </summary>
public sealed class RevokeRefreshTokenCommandHandler(IIdentityService identityService)
    : ICommandHandler<RevokeRefreshTokenCommand, Result<bool>>
{
    /// <inheritdoc />
    public Task<Result<bool>> HandleAsync(
        RevokeRefreshTokenCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.RevokeAsync(command.RefreshToken, cancellationToken);
}
