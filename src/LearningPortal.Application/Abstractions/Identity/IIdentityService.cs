using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Abstractions.Identity;

/// <summary>Defines authentication operations without exposing ASP.NET Identity.</summary>
public interface IIdentityService
{
    /// <summary>Validates credentials and issues an access and refresh token pair.</summary>
    Task<Result<AuthenticationResponse>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a standard student account and signs it in.</summary>
    Task<Result<AuthenticationResponse>> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Rotates an active refresh token and issues a new token pair.</summary>
    Task<Result<AuthenticationResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token when it exists.</summary>
    Task<Result<bool>> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
