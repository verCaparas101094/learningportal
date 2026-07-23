using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authentication.Commands.Login;
using LearningPortal.Application.Authentication.Commands.Refresh;
using LearningPortal.Application.Authentication.Commands.Revoke;
using LearningPortal.Application.Authentication.Commands.Register;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Shared.Identity;

namespace LearningPortal.Api.Endpoints;

/// <summary>
/// Maps authentication commands to anonymous Minimal API endpoints.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>
    /// Maps login, refresh-token rotation, and refresh-token revocation endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Authenticates a user and issues an access and refresh token pair.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Registers a standard student account and issues a token pair.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/me", GetMeAsync)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .Produces<CurrentUserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithSummary("Rotates an active refresh token and issues a new token pair.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/revoke", RevokeAsync)
            .AllowAnonymous()
            .WithName("RevokeRefreshToken")
            .WithSummary("Revokes a refresh token.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.SendAsync<LoginCommand, AuthenticationResponse>(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.SendAsync<RegisterCommand, AuthenticationResponse>(
            new RegisterCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.ConfirmPassword),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static IResult GetMeAsync(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new CurrentUserResponse(
            userId,
            currentUser.DisplayName ?? "Portal User",
            currentUser.Email ?? string.Empty,
            currentUser.Roles,
            true));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.SendAsync<RefreshTokenCommand, AuthenticationResponse>(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RevokeAsync(
        RevokeTokenRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.SendAsync<RevokeRefreshTokenCommand, bool>(
            new RevokeRefreshTokenCommand(request.RefreshToken),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }
}
