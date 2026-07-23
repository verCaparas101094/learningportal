using FluentValidation;

namespace LearningPortal.Application.UserManagement.Commands.SetUserEnabled;

/// <summary>Validates enabled-state change commands.</summary>
public sealed class SetUserEnabledCommandValidator : AbstractValidator<SetUserEnabledCommand>
{
    /// <summary>Initializes enabled-state validation rules.</summary>
    public SetUserEnabledCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
