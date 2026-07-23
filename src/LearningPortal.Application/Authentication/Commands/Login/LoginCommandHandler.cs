using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Login;

/// <summary>
/// Delegates credential authentication and token issuance to the identity abstraction.
/// </summary>
public sealed class LoginCommandHandler(IIdentityService identityService)
    : ICommandHandler<LoginCommand, Result<AuthenticationResponse>>
{
    /// <inheritdoc />
    public Task<Result<AuthenticationResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.LoginAsync(command.Email, command.Password, cancellationToken);
}
