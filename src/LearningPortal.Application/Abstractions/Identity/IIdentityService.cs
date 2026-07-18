using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Abstractions.Identity;

/// <summary>Defines authentication operations without exposing ASP.NET Identity.</summary>
public interface IIdentityService
{
    /// <summary>Validates credentials and issues an access token.</summary>
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
