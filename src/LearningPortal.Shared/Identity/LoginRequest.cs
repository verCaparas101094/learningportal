namespace LearningPortal.Shared.Identity;

/// <summary>Represents credentials submitted to the token endpoint.</summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginRequest(string Email, string Password);
