using FluentValidation;

namespace LearningPortal.Application.Authentication.Commands.Register;

/// <summary>Validates public self-registration input.</summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    /// <summary>Creates registration validation rules.</summary>
    public RegisterCommandValidator()
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(12);
        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password)
            .WithMessage("Password confirmation must match.");
    }
}
