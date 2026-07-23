namespace LearningPortal.Shared.UserManagement;

/// <summary>
/// Models an additive application-role assignment.
/// </summary>
/// <param name="Role">The application role to assign.</param>
public sealed record AssignUserRoleRequest(string Role);
