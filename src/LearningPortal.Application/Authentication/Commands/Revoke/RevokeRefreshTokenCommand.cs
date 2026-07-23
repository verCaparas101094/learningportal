using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Revoke;

/// <summary>
/// Requests revocation of a refresh token.
/// </summary>
/// <param name="RefreshToken">The raw refresh token supplied by the caller.</param>
public sealed record RevokeRefreshTokenCommand(string RefreshToken) : ICommand<Result<bool>>;
