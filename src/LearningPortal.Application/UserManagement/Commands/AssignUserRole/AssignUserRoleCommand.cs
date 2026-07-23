using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Commands.AssignUserRole;

/// <summary>Requests one additive application-role assignment.</summary>
public sealed record AssignUserRoleCommand(Guid UserId, string Role)
    : ICommand<Result<UserResponse>>;
