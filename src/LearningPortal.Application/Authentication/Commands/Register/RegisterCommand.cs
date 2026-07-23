using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Authentication.Commands.Register;

/// <summary>Registers a standard student account.</summary>
public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword)
    : ICommand<Result<AuthenticationResponse>>;
