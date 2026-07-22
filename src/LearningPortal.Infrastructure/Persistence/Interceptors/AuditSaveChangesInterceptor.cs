using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LearningPortal.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies audit metadata and converts tracked soft-deletable entities into logical deletions.
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    ICurrentUserService currentUserService,
    ISystemClock systemClock)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditState(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditState(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditState(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var utcNow = systemClock.UtcNow;
        var userId = currentUserService.IsAuthenticated ? currentUserService.UserId : null;

        foreach (var entry in dbContext.ChangeTracker.Entries()
                     .Where(entry => entry.Entity is AuditableEntity or ISoftDelete))
        {
            var isSoftDelete = entry.State == EntityState.Deleted && entry.Entity is ISoftDelete;

            if (isSoftDelete)
            {
                ConvertToSoftDelete(entry, utcNow, userId);
            }
            else if (entry.Entity is ISoftDelete)
            {
                ProtectSoftDeleteState(entry);
            }

            if (entry.Entity is AuditableEntity)
            {
                ApplyAuditMetadata(entry, utcNow, userId);
            }
        }
    }

    private static void ConvertToSoftDelete(
        EntityEntry entry,
        DateTimeOffset utcNow,
        Guid? userId)
    {
        entry.State = EntityState.Modified;
        entry.Property(nameof(ISoftDelete.IsDeleted)).CurrentValue = true;
        entry.Property(nameof(ISoftDelete.DeletedAtUtc)).CurrentValue = utcNow;
        entry.Property(nameof(ISoftDelete.DeletedBy)).CurrentValue = userId;
    }

    private static void ProtectSoftDeleteState(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            entry.Property(nameof(ISoftDelete.IsDeleted)).CurrentValue = false;
            entry.Property(nameof(ISoftDelete.DeletedAtUtc)).CurrentValue = null;
            entry.Property(nameof(ISoftDelete.DeletedBy)).CurrentValue = null;
            return;
        }

        if (entry.State == EntityState.Modified)
        {
            entry.Property(nameof(ISoftDelete.IsDeleted)).IsModified = false;
            entry.Property(nameof(ISoftDelete.DeletedAtUtc)).IsModified = false;
            entry.Property(nameof(ISoftDelete.DeletedBy)).IsModified = false;
        }
    }

    private static void ApplyAuditMetadata(
        EntityEntry entry,
        DateTimeOffset utcNow,
        Guid? userId)
    {
        entry.Property(nameof(AuditableEntity.RowVersion)).IsModified = false;

        if (entry.State == EntityState.Added)
        {
            entry.Property(nameof(AuditableEntity.CreatedAtUtc)).CurrentValue = utcNow;
            entry.Property(nameof(AuditableEntity.CreatedBy)).CurrentValue = userId;
            entry.Property(nameof(AuditableEntity.UpdatedAtUtc)).CurrentValue = null;
            entry.Property(nameof(AuditableEntity.UpdatedBy)).CurrentValue = null;
            return;
        }

        if (entry.State == EntityState.Modified)
        {
            entry.Property(nameof(AuditableEntity.CreatedAtUtc)).IsModified = false;
            entry.Property(nameof(AuditableEntity.CreatedBy)).IsModified = false;
            entry.Property(nameof(AuditableEntity.UpdatedAtUtc)).CurrentValue = utcNow;
            entry.Property(nameof(AuditableEntity.UpdatedBy)).CurrentValue = userId;
        }
    }
}
