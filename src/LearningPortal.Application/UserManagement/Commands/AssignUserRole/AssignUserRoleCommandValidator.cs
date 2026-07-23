using FluentValidation;
using LearningPortal.Application.Authorization;

namespace LearningPortal.Application.UserManagement.Commands.AssignUserRole;

/// <summary>Validates additive role-assignment commands.</summary>
public sealed class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    /// <summary>Initializes role-assignment validation rules.</summary>
    public AssignUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
        RuleFor(command => command.Role)
            .NotEmpty()
            .Must(ApplicationRoles.IsValid)
            .WithMessage("The specified role is not a valid application role.");
    }
}
