using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>Authenticates Identity users and issues signed JWT access tokens.</summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptions<JwtOptions> options,
    ISystemClock systemClock)
    : IIdentityService
{
    /// <inheritdoc />
    public async Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(request.Email);

        var signInResult = user is null
            ? null
            : await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (user is null || signInResult?.Succeeded != true)
        {
            return Result<TokenResponse>.Failure(Errors.Authentication.InvalidCredentials());
        }

        var jwtOptions = options.Value;
        var now = systemClock.UtcNow;
        var expiresAt = now.AddMinutes(jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? request.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName)
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Result<TokenResponse>.Success(new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt));
    }
}
