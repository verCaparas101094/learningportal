using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Queries.GetUsers;

/// <summary>Requests one filtered, paginated page of Identity users.</summary>
public sealed record GetUsersQuery(
    string? Search,
    int PageNumber,
    int PageSize)
    : IQuery<Result<PagedUsersResponse>>;
