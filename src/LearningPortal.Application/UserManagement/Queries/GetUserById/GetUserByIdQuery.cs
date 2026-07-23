using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Queries.GetUserById;

/// <summary>Requests one Identity user by identifier.</summary>
public sealed record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserResponse>>;
