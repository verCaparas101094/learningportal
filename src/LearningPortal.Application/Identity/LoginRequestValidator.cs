using FluentValidation;
using LearningPortal.Shared.Identity;

namespace LearningPortal.Application.Identity;

/// <summary>Validates credentials before querying the Identity store.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Initializes login request rules.</summary>
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(256);
    }
}
