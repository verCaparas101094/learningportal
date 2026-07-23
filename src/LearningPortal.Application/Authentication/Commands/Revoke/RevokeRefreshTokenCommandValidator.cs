using FluentValidation;

namespace LearningPortal.Application.Authentication.Commands.Revoke;

/// <summary>
/// Validates refresh-token revocation commands.
/// </summary>
public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    /// <summary>Initializes the refresh-token revocation validation rules.</summary>
    public RevokeRefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
