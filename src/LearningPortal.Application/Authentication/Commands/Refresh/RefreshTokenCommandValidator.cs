using FluentValidation;

namespace LearningPortal.Application.Authentication.Commands.Refresh;

/// <summary>
/// Validates refresh-token rotation commands.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Initializes the refresh-token validation rules.</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
