using FluentValidation;

namespace LearningPortal.Application.Authentication.Commands.Login;

/// <summary>
/// Validates login commands before authentication is attempted.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initializes the login validation rules.</summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(256);
    }
}
