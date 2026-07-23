using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LearningPortal.Infrastructure.Identity;

/// <summary>
/// Authenticates Identity users and manages persisted refresh-token lifecycles.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenProtector refreshTokenProtector,
    IOptions<JwtOptions> options,
    ISystemClock systemClock,
    ILogger<IdentityService> logger)
    : IIdentityService
{
    /// <inheritdoc />
    public async Task<Result<TokenResponse>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);
        var signInResult = user is null
            ? null
            : await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (user is null || signInResult?.Succeeded != true)
        {
            return Result<TokenResponse>.Failure(Errors.Authentication.InvalidCredentials());
        }

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<TokenResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<TokenResponse>.Failure(Errors.Authentication.InvalidRefreshToken());
        }

        var tokenHash = refreshTokenProtector.Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return Result<TokenResponse>.Failure(Errors.Authentication.InvalidRefreshToken());
        }

        var utcNow = systemClock.UtcNow;
        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeAllActiveTokensAsync(storedToken.UserId, utcNow, cancellationToken);
            logger.LogWarning(
                "Refresh token replay detected for user {UserId}; all active refresh tokens were revoked.",
                storedToken.UserId);
            return Result<TokenResponse>.Failure(Errors.Authentication.RefreshTokenReused());
        }

        if (!storedToken.IsActive(utcNow))
        {
            storedToken.Revoke(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<TokenResponse>.Failure(Errors.Authentication.RefreshTokenExpired());
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null)
        {
            storedToken.Revoke(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<TokenResponse>.Failure(Errors.Authentication.InvalidRefreshToken());
        }

        var replacementMaterial = refreshTokenProtector.Generate();
        var replacementExpiresAtUtc = utcNow.AddDays(options.Value.RefreshTokenExpirationDays);
        var replacement = RefreshToken.Create(
            user.Id,
            replacementMaterial.TokenHash,
            utcNow,
            replacementExpiresAtUtc);
        var accessToken = await accessTokenGenerator.GenerateAsync(user, cancellationToken);

        storedToken.Rotate(replacementMaterial.TokenHash, utcNow);
        await dbContext.RefreshTokens.AddAsync(replacement, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(
                exception,
                "Concurrent refresh token rotation detected for user {UserId}.",
                storedToken.UserId);

            dbContext.ChangeTracker.Clear();
            await RevokeAllActiveTokensAsync(storedToken.UserId, utcNow, cancellationToken);
            return Result<TokenResponse>.Failure(Errors.Authentication.RefreshTokenReused());
        }

        logger.LogInformation("Rotated refresh token for user {UserId}.", user.Id);
        return Result<TokenResponse>.Success(new TokenResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            replacementMaterial.RawToken,
            replacementExpiresAtUtc));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<bool>.Failure(Errors.Authentication.InvalidRefreshToken());
        }

        var tokenHash = refreshTokenProtector.Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return Result<bool>.Success(true);
        }

        if (storedToken.RevokedAtUtc is null)
        {
            storedToken.Revoke(systemClock.UtcNow);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
                await RevokeAllActiveTokensAsync(storedToken.UserId, systemClock.UtcNow, cancellationToken);
            }
        }

        logger.LogInformation("Revoked refresh token for user {UserId}.", storedToken.UserId);
        return Result<bool>.Success(true);
    }

    private async Task<Result<TokenResponse>> IssueTokenPairAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var utcNow = systemClock.UtcNow;
        var refreshToken = refreshTokenProtector.Generate();
        var refreshTokenExpiresAtUtc = utcNow.AddDays(options.Value.RefreshTokenExpirationDays);
        var persistedToken = RefreshToken.Create(
            user.Id,
            refreshToken.TokenHash,
            utcNow,
            refreshTokenExpiresAtUtc);
        var accessToken = await accessTokenGenerator.GenerateAsync(user, cancellationToken);

        await dbContext.RefreshTokens.AddAsync(persistedToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Issued authentication token pair for user {UserId}.", user.Id);
        return Result<TokenResponse>.Success(new TokenResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.RawToken,
            refreshTokenExpiresAtUtc));
    }

    private async Task RevokeAllActiveTokensAsync(
        Guid userId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
                cancellationToken);
    }
}
