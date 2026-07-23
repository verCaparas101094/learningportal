using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Implements secure refresh-token generation and SHA-256 hashing.
/// </summary>
public sealed class RefreshTokenProtector : IRefreshTokenProtector
{
    private const int TokenSizeInBytes = 64;

    /// <inheritdoc />
    public GeneratedRefreshToken Generate()
    {
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        return new GeneratedRefreshToken(rawToken, Hash(rawToken));
    }

    /// <inheritdoc />
    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    /// <inheritdoc />
    public bool Matches(string rawValue, string expectedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);

        var actualBytes = Convert.FromHexString(Hash(rawValue));

        try
        {
            var expectedBytes = Convert.FromHexString(expectedHash);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
