using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Refresh;

/// <summary>
/// Requests rotation of an active refresh token.
/// </summary>
/// <param name="RefreshToken">The raw refresh token supplied by the caller.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<Result<TokenResponse>>;
