using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authentication.Commands.Login;
using LearningPortal.Application.Authentication.Commands.Refresh;
using LearningPortal.Application.Authentication.Commands.Revoke;
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
            .WithTags("Authentication")
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticates a user and issues an access and refresh token pair.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Rotates an active refresh token and issues a new token pair.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/revoke", RevokeAsync)
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
