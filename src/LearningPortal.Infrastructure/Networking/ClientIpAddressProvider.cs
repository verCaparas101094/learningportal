using LearningPortal.Application.Abstractions.Networking;
using Microsoft.AspNetCore.Http;

namespace LearningPortal.Infrastructure.Networking;

/// <summary>
/// Resolves the current connection's remote IP address through <see cref="IHttpContextAccessor"/>.
/// </summary>
public sealed class ClientIpAddressProvider(IHttpContextAccessor httpContextAccessor)
    : IClientIpAddressProvider
{
    private const string UnknownAddress = "unknown";

    /// <inheritdoc />
    public string IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? UnknownAddress;
}
