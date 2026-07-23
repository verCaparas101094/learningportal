using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>
/// Defines secure SQL Server persistence and concurrency mapping for refresh tokens.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(token => token.SecurityStampHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(token => token.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasPrecision(0).IsRequired();
        builder.Property(token => token.RevokedAtUtc).HasPrecision(0);
        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(64)
            .IsUnicode(false);
        builder.Property(token => token.CreatedByIp).HasMaxLength(45).IsRequired();
        builder.Property(token => token.RevokedByIp).HasMaxLength(45);
        builder.Property(token => token.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.ExpiresAtUtc);
        builder.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
