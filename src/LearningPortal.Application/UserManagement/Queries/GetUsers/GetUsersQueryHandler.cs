using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Application.UserManagement.Queries.GetUsers;

/// <summary>Validates and delegates paginated Identity user reads.</summary>
public sealed class GetUsersQueryHandler(
    IUserManagementService userManagementService,
    IValidator<GetUsersQuery> validator)
    : IQueryHandler<GetUsersQuery, Result<PagedUsersResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedUsersResponse>> HandleAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join(
                " ",
                validation.Errors
                    .Select(failure => failure.ErrorMessage)
                    .Distinct(StringComparer.Ordinal));
            return Result<PagedUsersResponse>.Failure(Errors.Validation.Failed(message));
        }

        return await userManagementService.GetUsersAsync(
            query.Search,
            query.PageNumber,
            query.PageSize,
            cancellationToken);
    }
}
