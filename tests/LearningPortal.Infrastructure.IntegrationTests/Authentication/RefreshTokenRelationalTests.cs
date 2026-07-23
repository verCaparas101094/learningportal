using System.Data.Common;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.IntegrationTests.Authentication;

/// <summary>
/// Verifies refresh-token transactions, SQL Server rowversion concurrency, constraints, and migrations.
/// </summary>
public sealed class RefreshTokenRelationalTests(
    SqlServerAuthenticationFixture fixture)
    : IClassFixture<SqlServerAuthenticationFixture>
{
    /// <summary>Verifies that rotation commits successfully through a real SQL Server transaction.</summary>
    [SqlServerFact]
    [Trait("Category", "Integration")]
    public async Task RefreshAsync_UsesRelationalTransaction_AndRotatesToken()
    {
        var seed = await fixture.CreateAuthenticationAsync();
        await using var scope = fixture.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var result = await identityService.RefreshAsync(seed.Authentication.RefreshToken);

        Assert.True(result.IsSuccess);
        await using var verificationScope = fixture.CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protector = verificationScope.ServiceProvider.GetRequiredService<IRefreshTokenProtector>();
        var originalHash = protector.Hash(seed.Authentication.RefreshToken);
        var original = await context.RefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.TokenHash == originalHash);
        Assert.True(original.IsRevoked);
        Assert.NotNull(original.ReplacedByTokenHash);
    }

    /// <summary>
    /// Verifies that two independent contexts produce one success, one replay failure, and a revoked replacement.
    /// </summary>
    [SqlServerFact]
    [Trait("Category", "Integration")]
    public async Task RefreshAsync_WithConcurrentContexts_AllowsExactlyOneRotation()
    {
        var seed = await fixture.CreateAuthenticationAsync();
        await using var firstScope = fixture.CreateScope();
        await using var secondScope = fixture.CreateScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<IIdentityService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IIdentityService>();
        fixture.Coordinator.Arm(2);

        try
        {
            var results = await Task.WhenAll(
                firstService.RefreshAsync(seed.Authentication.RefreshToken),
                secondService.RefreshAsync(seed.Authentication.RefreshToken));

            var successful = Assert.Single(results, result => result.IsSuccess);
            var replayed = Assert.Single(results, result => result.IsFailure);
            Assert.Equal("Authentication.RefreshTokenReplayDetected", replayed.Error?.Code);

            await using var verificationScope = fixture.CreateScope();
            var context = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var protector = verificationScope.ServiceProvider.GetRequiredService<IRefreshTokenProtector>();
            var replacementHash = protector.Hash(successful.Value.RefreshToken);
            var replacement = await context.RefreshTokens
                .AsNoTracking()
                .SingleAsync(token => token.TokenHash == replacementHash);
            Assert.True(replacement.IsRevoked);
        }
        finally
        {
            fixture.Coordinator.Disarm();
        }
    }

    /// <summary>Verifies that SQL Server enforces the unique refresh-token hash constraint.</summary>
    [SqlServerFact]
    [Trait("Category", "Integration")]
    public async Task RefreshTokens_WithDuplicateHash_ViolateUniqueConstraint()
    {
        var seed = await fixture.CreateAuthenticationAsync();
        await using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IRefreshTokenProtector>();
        var existingHash = protector.Hash(seed.Authentication.RefreshToken);
        var existing = await context.RefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.TokenHash == existingHash);
        var duplicate = RefreshToken.Create(
            seed.UserId,
            existing.TokenHash,
            existing.SecurityStampHash,
            "203.0.113.21",
            existing.CreatedAtUtc,
            existing.ExpiresAtUtc);

        await context.RefreshTokens.AddAsync(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    /// <summary>Verifies that migrations create rowversion and every required refresh-token index.</summary>
    [SqlServerFact]
    [Trait("Category", "Integration")]
    public async Task Migration_CreatesRefreshTokenConcurrencyColumnAndIndexes()
    {
        await fixture.EnsureInitializedAsync();
        await using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var rowVersionCount = await ExecuteScalarAsync(
            connection,
            """
            SELECT COUNT(*)
            FROM sys.columns AS columns
            INNER JOIN sys.types AS types ON columns.system_type_id = types.system_type_id
                AND types.user_type_id = types.system_type_id
            WHERE columns.object_id = OBJECT_ID(N'[dbo].[RefreshTokens]')
                AND columns.name = N'RowVersion'
                AND types.name = N'timestamp'
                AND columns.is_nullable = 0;
            """);
        var indexes = await ReadIndexesAsync(connection);

        Assert.Equal(1, Convert.ToInt32(rowVersionCount));
        Assert.True(indexes["IX_RefreshTokens_TokenHash"]);
        Assert.False(indexes["IX_RefreshTokens_ExpiresAtUtc"]);
        Assert.False(indexes["IX_RefreshTokens_UserId_ExpiresAtUtc"]);
    }

    private static async Task<object?> ExecuteScalarAsync(
        DbConnection connection,
        string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return await command.ExecuteScalarAsync();
    }

    private static async Task<Dictionary<string, bool>> ReadIndexesAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT name, is_unique
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]')
                AND name IS NOT NULL;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var indexes = new Dictionary<string, bool>(StringComparer.Ordinal);

        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0), reader.GetBoolean(1));
        }

        return indexes;
    }
}
