using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Generates signed JWT access tokens with the portal's required identity claims.
/// </summary>
public sealed class JwtAccessTokenGenerator(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    ISystemClock systemClock)
    : IAccessTokenGenerator
{
    /// <inheritdoc />
    public async Task<GeneratedAccessToken> GenerateAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        var jwtOptions = options.Value;
        var now = systemClock.UtcNow;
        var expiresAtUtc = now.AddMinutes(jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(ApplicationClaimTypes.UserId, user.Id.ToString()),
            new(ApplicationClaimTypes.DisplayName, string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? user.UserName ?? user.Id.ToString() : user.DisplayName),
            new(ApplicationClaimTypes.Email, user.Email ?? string.Empty),
            new(ApplicationClaimTypes.JwtId, Guid.CreateVersion7().ToString()),
            new(
                ApplicationClaimTypes.IssuedAt,
                now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(
            roles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(role => new Claim(ApplicationClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new GeneratedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }
}
