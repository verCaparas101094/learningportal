using FluentValidation;
using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Shared.Identity;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps authentication endpoints.</summary>
public static class IdentityEndpoints
{
    /// <summary>Maps endpoints that issue access tokens.</summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/token", LoginAsync)
            .AllowAnonymous()
            .WithName("IssueAccessToken")
            .WithSummary("Authenticates a user and returns a JWT access token.")
            .Produces<TokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IIdentityService identityService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await identityService.LoginAsync(request, cancellationToken);
        return result.ToHttpResult();
    }
}
