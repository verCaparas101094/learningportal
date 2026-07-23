using System.IdentityModel.Tokens.Jwt;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authentication;

/// <summary>
/// Verifies authentication, refresh-token lifecycle, replay protection, and JWT claims.
/// </summary>
public sealed class IdentityServiceTests
{
    /// <summary>Verifies that valid credentials issue and persist a token pair.</summary>
    [Fact]
    public async Task LoginAsync_WithValidCredentials_IssuesTokenPair()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();

        var result = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
        Assert.True(result.Value.AccessTokenExpiresAtUtc > context.Clock.UtcNow);
        Assert.True(result.Value.RefreshTokenExpiresAtUtc > result.Value.AccessTokenExpiresAtUtc);
        Assert.Equal(1, await context.DbContext.RefreshTokens.CountAsync());
    }

    /// <summary>Verifies that invalid credentials return a stable non-disclosing failure.</summary>
    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsInvalidCredentials()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();

        var result = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            "IncorrectPassword!123");

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidCredentials", result.Error?.Code);
    }

    /// <summary>Verifies that a locked user with valid credentials receives the locked-user failure.</summary>
    [Fact]
    public async Task LoginAsync_WithLockedUser_ReturnsUserLocked()
    {
        await using var context = await AuthenticationTestContext.CreateAsync(isLockedOut: true);

        var result = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.UserLocked", result.Error?.Code);
    }

    /// <summary>Verifies that persistence contains only a hash and never the raw refresh token.</summary>
    [Fact]
    public async Task LoginAsync_PersistsOnlyRefreshTokenHash()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var result = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);

        context.DbContext.ChangeTracker.Clear();
        var persistedToken = await context.DbContext.RefreshTokens.SingleAsync();

        Assert.NotEqual(result.Value.RefreshToken, persistedToken.TokenHash);
        Assert.Equal(
            context.RefreshTokenProtector.Hash(result.Value.RefreshToken),
            persistedToken.TokenHash);
        Assert.DoesNotContain(
            typeof(RefreshToken).GetProperties(),
            property => property.Name is "RawToken" or "RefreshToken");
    }

    /// <summary>Verifies successful refresh-token rotation and replacement linkage.</summary>
    [Fact]
    public async Task RefreshAsync_WithActiveToken_RotatesToken()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var login = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);

        var refresh = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);

        Assert.True(refresh.IsSuccess);
        Assert.NotEqual(login.Value.RefreshToken, refresh.Value.RefreshToken);
        context.DbContext.ChangeTracker.Clear();
        var originalHash = context.RefreshTokenProtector.Hash(login.Value.RefreshToken);
        var replacementHash = context.RefreshTokenProtector.Hash(refresh.Value.RefreshToken);
        var original = await context.DbContext.RefreshTokens.SingleAsync(token => token.TokenHash == originalHash);
        Assert.True(original.IsRevoked);
        Assert.Equal(replacementHash, original.ReplacedByTokenHash);
    }

    /// <summary>Verifies that the previous token cannot be used after successful rotation.</summary>
    [Fact]
    public async Task RefreshAsync_WithRotatedToken_ReturnsReplayDetected()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var login = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);
        _ = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);

        var replay = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);

        Assert.True(replay.IsFailure);
        Assert.Equal("Authentication.RefreshTokenReplayDetected", replay.Error?.Code);
    }

    /// <summary>Verifies that replay detection revokes the user's replacement token.</summary>
    [Fact]
    public async Task RefreshAsync_WhenReplayDetected_RevokesActiveTokens()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var login = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);
        var rotated = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);

        _ = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);

        context.DbContext.ChangeTracker.Clear();
        var replacementHash = context.RefreshTokenProtector.Hash(rotated.Value.RefreshToken);
        var replacement = await context.DbContext.RefreshTokens.SingleAsync(
            token => token.TokenHash == replacementHash);
        Assert.True(replacement.IsRevoked);
    }

    /// <summary>Verifies that expired refresh tokens are rejected and revoked.</summary>
    [Fact]
    public async Task RefreshAsync_WithExpiredToken_ReturnsExpiredFailure()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var material = context.RefreshTokenProtector.Generate();
        var securityStamp = await context.UserManager.GetSecurityStampAsync(context.User);
        var token = RefreshToken.Create(
            context.User.Id,
            material.TokenHash,
            context.RefreshTokenProtector.Hash(securityStamp),
            "203.0.113.10",
            context.Clock.UtcNow.AddDays(-2),
            context.Clock.UtcNow.AddDays(-1));
        await context.DbContext.RefreshTokens.AddAsync(token);
        await context.DbContext.SaveChangesAsync();

        var result = await context.IdentityService.RefreshAsync(material.RawToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.RefreshTokenExpired", result.Error?.Code);
        context.DbContext.ChangeTracker.Clear();
        var persistedToken = await context.DbContext.RefreshTokens
            .SingleAsync(storedToken => storedToken.TokenHash == material.TokenHash);
        Assert.True(persistedToken.IsRevoked);
    }

    /// <summary>Verifies that revocation succeeds repeatedly without revealing token existence.</summary>
    [Fact]
    public async Task RevokeAsync_WhenRepeated_RemainsSuccessful()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var login = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);

        var first = await context.IdentityService.RevokeAsync(login.Value.RefreshToken);
        var second = await context.IdentityService.RevokeAsync(login.Value.RefreshToken);
        var unknown = await context.IdentityService.RevokeAsync("unknown-refresh-token");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(unknown.IsSuccess);
    }

    /// <summary>Verifies that the same token can produce at most one successful rotation.</summary>
    [Fact]
    public async Task RefreshAsync_WithSameTokenTwice_OnlyFirstRotationSucceeds()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var login = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);

        var first = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);
        var second = await context.IdentityService.RefreshAsync(login.Value.RefreshToken);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
    }

    /// <summary>Verifies that generated JWTs contain every required authentication claim.</summary>
    [Fact]
    public async Task LoginAsync_GeneratedJwtContainsRequiredClaims()
    {
        await using var context = await AuthenticationTestContext.CreateAsync();
        var result = await context.IdentityService.LoginAsync(
            AuthenticationTestContext.Email,
            AuthenticationTestContext.Password);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);

        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub);
        Assert.Contains(token.Claims, claim => claim.Type == "name");
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email);
        Assert.Contains(token.Claims, claim => claim.Type == "role" && claim.Value == AuthenticationTestContext.Role);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Iat);
    }
}
