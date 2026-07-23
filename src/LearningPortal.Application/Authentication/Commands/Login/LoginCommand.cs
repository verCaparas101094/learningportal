using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Login;

/// <summary>
/// Requests authentication with an email address and password.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginCommand(string Email, string Password) : ICommand<Result<TokenResponse>>;
