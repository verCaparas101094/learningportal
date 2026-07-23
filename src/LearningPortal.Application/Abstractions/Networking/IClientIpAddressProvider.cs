namespace LearningPortal.Application.Abstractions.Networking;

/// <summary>
/// Provides the current client IP address without exposing HTTP abstractions to Application.
/// </summary>
public interface IClientIpAddressProvider
{
    /// <summary>
    /// Gets the normalized client IP address or a stable fallback when no client address exists.
    /// </summary>
    string IpAddress { get; }
}
