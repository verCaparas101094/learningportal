using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Commands.SetUserEnabled;

/// <summary>Requests an enabled-state change for one Identity user.</summary>
public sealed record SetUserEnabledCommand(Guid UserId, bool IsEnabled)
    : ICommand<Result<UserResponse>>;
