using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Refresh;

/// <summary>
/// Delegates refresh-token rotation to the identity abstraction.
/// </summary>
public sealed class RefreshTokenCommandHandler(IIdentityService identityService)
    : ICommandHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    /// <inheritdoc />
    public Task<Result<TokenResponse>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.RefreshAsync(command.RefreshToken, cancellationToken);
}
