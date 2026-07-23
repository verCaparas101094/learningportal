using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.UserManagement.Commands.AssignUserRole;
using LearningPortal.Application.UserManagement.Commands.SetUserEnabled;
using LearningPortal.Application.UserManagement.Queries.GetUserById;
using LearningPortal.Application.UserManagement.Queries.GetUsers;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;

namespace LearningPortal.Api.Endpoints;

/// <summary>
/// Maps the compact administrator-only user-management API.
/// </summary>
public static class UserManagementEndpoints
{
    /// <summary>Maps user listing, lookup, enabled-state, and additive role operations.</summary>
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users")
            .WithTags("User Management")
            .RequireAdministrator();

        group.MapGet("/", GetUsersAsync)
            .WithName("GetUsers")
            .WithSummary("Returns a filtered and paginated user list.")
            .Produces<PagedUsersResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{userId:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Returns one user by identifier.")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/enable", EnableUserAsync)
            .WithName("EnableUser")
            .WithSummary("Enables one user.")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/disable", DisableUserAsync)
            .WithName("DisableUser")
            .WithSummary("Disables one user.")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/roles", AssignRoleAsync)
            .WithName("AssignUserRole")
            .WithSummary("Adds one valid application role without removing existing roles.")
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetUsersAsync(
        [AsParameters] GetUsersRequest request,
        IQueryHandler<GetUsersQuery, Result<PagedUsersResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(request.Search, request.PageNumber, request.PageSize);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUserByIdAsync(
        Guid userId,
        IQueryHandler<GetUserByIdQuery, Result<UserResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserByIdQuery(userId), cancellationToken);
        return result.ToHttpResult();
    }

    private static Task<IResult> EnableUserAsync(
        Guid userId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken) =>
        SetEnabledAsync(userId, true, commandDispatcher, cancellationToken);

    private static Task<IResult> DisableUserAsync(
        Guid userId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken) =>
        SetEnabledAsync(userId, false, commandDispatcher, cancellationToken);

    private static async Task<IResult> SetEnabledAsync(
        Guid userId,
        bool isEnabled,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.SendAsync<SetUserEnabledCommand, UserResponse>(
            new SetUserEnabledCommand(userId, isEnabled),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignRoleAsync(
        Guid userId,
        AssignUserRoleRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.SendAsync<AssignUserRoleCommand, UserResponse>(
            new AssignUserRoleCommand(userId, request.Role),
            cancellationToken);
        return result.ToHttpResult();
    }
}
