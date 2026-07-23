using System.Data;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Networking;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenProtector refreshTokenProtector,
    IClientIpAddressProvider clientIpAddressProvider,
    IOptions<JwtOptions> options,
    ISystemClock systemClock,
    ILogger<IdentityService> logger)
    : IIdentityService
{
    /// <inheritdoc />
    public async Task<Result<AuthenticationResponse>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return Result<AuthenticationResponse>.Failure(Errors.Authentication.InvalidCredentials());
        }

        if (!user.IsEnabled)
        {
            return await PasswordMatchesAsync(user, password)
                ? Result<AuthenticationResponse>.Failure(Errors.Authentication.UserUnavailable())
                : Result<AuthenticationResponse>.Failure(Errors.Authentication.InvalidCredentials());
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (signInResult.Succeeded)
        {
            return await IssueTokenPairAsync(user, cancellationToken);
        }

        if (signInResult.IsLockedOut && await PasswordMatchesAsync(user, password))
        {
            return Result<AuthenticationResponse>.Failure(Errors.Authentication.UserLocked());
        }

        if (signInResult.IsNotAllowed && await PasswordMatchesAsync(user, password))
        {
            return Result<AuthenticationResponse>.Failure(Errors.Authentication.UserUnavailable());
        }

        return Result<AuthenticationResponse>.Failure(Errors.Authentication.InvalidCredentials());
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthenticationResponse>.Failure(Errors.Authentication.InvalidRefreshToken());
        }

        var tokenHash = refreshTokenProtector.Hash(refreshToken);
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(
                tokenHash,
                ExecuteRefreshRotationAsync,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            return await HandleRefreshReplayAsync(tokenHash, exception, cancellationToken);
        }
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
        var utcNow = systemClock.UtcNow;
        var clientIpAddress = clientIpAddressProvider.IpAddress;

        var affectedRows = await RevokeByHashAsync(
            tokenHash,
            utcNow,
            clientIpAddress,
            cancellationToken);

        if (affectedRows > 0)
        {
            logger.LogInformation("A refresh token was revoked successfully.");
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<AuthenticationResponse>> IssueTokenPairAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var securityStampHash = await GetSecurityStampHashAsync(user);
        if (securityStampHash is null)
        {
            return Result<AuthenticationResponse>.Failure(Errors.Authentication.UserUnavailable());
        }

        var utcNow = systemClock.UtcNow;
        var refreshToken = refreshTokenProtector.Generate();
        var refreshTokenExpiresAtUtc = utcNow.AddDays(options.Value.RefreshTokenExpirationDays);
        var persistedToken = RefreshToken.Create(
            user.Id,
            refreshToken.TokenHash,
            securityStampHash,
            clientIpAddressProvider.IpAddress,
            utcNow,
            refreshTokenExpiresAtUtc);
        var accessToken = await accessTokenGenerator.GenerateAsync(user, cancellationToken);

        await dbContext.RefreshTokens.AddAsync(persistedToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Issued authentication token pair for user {UserId}.", user.Id);
        return Result<AuthenticationResponse>.Success(new AuthenticationResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.RawToken,
            refreshTokenExpiresAtUtc));
    }

    private async Task<Error?> ValidateRefreshUserAsync(
        ApplicationUser? user,
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user is null || !user.IsEnabled || !await signInManager.CanSignInAsync(user))
        {
            return Errors.Authentication.UserUnavailable();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Errors.Authentication.UserLocked();
        }

        var securityStamp = await GetSecurityStampAsync(user);
        return securityStamp is null || !refreshTokenProtector.Matches(securityStamp, refreshToken.SecurityStampHash)
            ? Errors.Authentication.UserUnavailable()
            : null;
    }

    private async Task<string?> GetSecurityStampHashAsync(ApplicationUser user)
    {
        var securityStamp = await GetSecurityStampAsync(user);
        return securityStamp is null ? null : refreshTokenProtector.Hash(securityStamp);
    }

    private async Task<string?> GetSecurityStampAsync(ApplicationUser user)
    {
        if (!userManager.SupportsUserSecurityStamp)
        {
            return null;
        }

        var securityStamp = await userManager.GetSecurityStampAsync(user);
        return string.IsNullOrWhiteSpace(securityStamp) ? null : securityStamp;
    }

    private Task<bool> PasswordMatchesAsync(ApplicationUser user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    private async Task<Result<AuthenticationResponse>> ExecuteRefreshRotationAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        IDbContextTransaction? transaction = null;

        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);
            }

            var storedToken = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

            if (storedToken is null)
            {
                return Result<AuthenticationResponse>.Failure(Errors.Authentication.InvalidRefreshToken());
            }

            var utcNow = systemClock.UtcNow;
            var clientIpAddress = clientIpAddressProvider.IpAddress;

            if (storedToken.IsRevoked)
            {
                await RevokeAllActiveTokensAsync(
                    dbContext,
                    storedToken.UserId,
                    utcNow,
                    clientIpAddress,
                    cancellationToken);
                await CommitAsync(transaction, cancellationToken);

                logger.LogWarning(
                    "Previously revoked refresh token presented for user {UserId}; active refresh tokens were revoked. Rotated: {WasRotated}.",
                    storedToken.UserId,
                    storedToken.ReplacedByTokenHash is not null);

                var error = storedToken.ReplacedByTokenHash is not null
                    ? Errors.Authentication.RefreshTokenReused()
                    : Errors.Authentication.RefreshTokenRevoked();
                return Result<AuthenticationResponse>.Failure(error);
            }

            if (storedToken.IsExpired(utcNow))
            {
                storedToken.Revoke(utcNow, clientIpAddress);
                await dbContext.SaveChangesAsync(cancellationToken);
                await CommitAsync(transaction, cancellationToken);
                return Result<AuthenticationResponse>.Failure(Errors.Authentication.RefreshTokenExpired());
            }

            var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
            var userError = await ValidateRefreshUserAsync(user, storedToken, cancellationToken);

            if (userError is not null)
            {
                await RevokeAllActiveTokensAsync(
                    dbContext,
                    storedToken.UserId,
                    utcNow,
                    clientIpAddress,
                    cancellationToken);
                await CommitAsync(transaction, cancellationToken);
                return Result<AuthenticationResponse>.Failure(userError);
            }

            var authenticatedUser = user!;
            var replacementMaterial = refreshTokenProtector.Generate();
            var replacementExpiresAtUtc = utcNow.AddDays(options.Value.RefreshTokenExpirationDays);
            var replacement = RefreshToken.Create(
                authenticatedUser.Id,
                replacementMaterial.TokenHash,
                storedToken.SecurityStampHash,
                clientIpAddress,
                utcNow,
                replacementExpiresAtUtc);
            var accessToken = await accessTokenGenerator.GenerateAsync(authenticatedUser, cancellationToken);

            storedToken.Rotate(replacementMaterial.TokenHash, utcNow, clientIpAddress);
            await dbContext.RefreshTokens.AddAsync(replacement, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, cancellationToken);

            logger.LogInformation("Rotated refresh token for user {UserId}.", authenticatedUser.Id);
            return Result<AuthenticationResponse>.Success(new AuthenticationResponse(
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                replacementMaterial.RawToken,
                replacementExpiresAtUtc));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<Result<AuthenticationResponse>> HandleRefreshReplayAsync(
        string tokenHash,
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        await using var recoveryContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var executionStrategy = recoveryContext.Database.CreateExecutionStrategy();
        var replayedUserId = await executionStrategy.ExecuteAsync(
            async retryCancellationToken =>
            {
                recoveryContext.ChangeTracker.Clear();
                IDbContextTransaction? transaction = null;

                try
                {
                    if (recoveryContext.Database.IsRelational())
                    {
                        transaction = await recoveryContext.Database.BeginTransactionAsync(
                            IsolationLevel.ReadCommitted,
                            retryCancellationToken);
                    }

                    var replayedToken = await recoveryContext.RefreshTokens
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            token => token.TokenHash == tokenHash,
                            retryCancellationToken);

                    if (replayedToken is not null)
                    {
                        await RevokeAllActiveTokensAsync(
                            recoveryContext,
                            replayedToken.UserId,
                            systemClock.UtcNow,
                            clientIpAddressProvider.IpAddress,
                            retryCancellationToken);
                        await recoveryContext.SaveChangesAsync(retryCancellationToken);
                    }

                    await CommitAsync(transaction, retryCancellationToken);
                    return replayedToken?.UserId;
                }
                finally
                {
                    if (transaction is not null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            },
            cancellationToken);

        logger.LogWarning(
            exception,
            "Concurrent refresh-token rotation detected for user {UserId}; active refresh tokens were revoked using a fresh database context.",
            replayedUserId);

        return Result<AuthenticationResponse>.Failure(Errors.Authentication.RefreshTokenReused());
    }

    private static Task CommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask;

    private static async Task RevokeAllActiveTokensAsync(
        ApplicationDbContext context,
        Guid userId,
        DateTimeOffset revokedAtUtc,
        string revokedByIp,
        CancellationToken cancellationToken)
    {
        var query = context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null);

        if (context.Database.IsRelational())
        {
            await query.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(token => token.RevokedByIp, revokedByIp),
                cancellationToken);
            return;
        }

        var activeTokens = await query.ToListAsync(cancellationToken);
        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(revokedAtUtc, revokedByIp);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> RevokeByHashAsync(
        string tokenHash,
        DateTimeOffset revokedAtUtc,
        string revokedByIp,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAtUtc == null);

        if (dbContext.Database.IsRelational())
        {
            return await query.ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(token => token.RevokedByIp, revokedByIp),
                cancellationToken);
        }

        var token = await query.SingleOrDefaultAsync(cancellationToken);
        if (token is null)
        {
            return 0;
        }

        token.Revoke(revokedAtUtc, revokedByIp);
        await dbContext.SaveChangesAsync(cancellationToken);
        return 1;
    }
}
