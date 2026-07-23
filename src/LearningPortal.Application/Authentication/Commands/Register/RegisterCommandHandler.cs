using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Register;

/// <summary>Registers standard learner accounts through ASP.NET Identity.</summary>
public sealed class RegisterCommandHandler(IIdentityService identityService)
    : ICommandHandler<RegisterCommand, Result<AuthenticationResponse>>
{
    /// <inheritdoc />
    public Task<Result<AuthenticationResponse>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.RegisterAsync(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Password,
            cancellationToken);
}
