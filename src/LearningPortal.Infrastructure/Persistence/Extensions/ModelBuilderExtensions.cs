using System.Linq.Expressions;
using LearningPortal.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Extensions;

/// <summary>
/// Provides centralized EF Core conventions for domain foundation types.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures audit columns, SQL Server rowversion concurrency, and soft-delete query filters.
    /// </summary>
    /// <param name="modelBuilder">The model builder being configured.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ConfigureDomainFoundation(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var rootEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => entityType.BaseType is null)
            .ToArray();

        foreach (var entityType in rootEntityTypes)
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var entity = modelBuilder.Entity(entityType.ClrType);
                entity.Property<DateTimeOffset>(nameof(AuditableEntity.CreatedAtUtc)).HasPrecision(0).IsRequired();
                entity.Property<Guid?>(nameof(AuditableEntity.CreatedBy));
                entity.Property<DateTimeOffset?>(nameof(AuditableEntity.UpdatedAtUtc)).HasPrecision(0);
                entity.Property<Guid?>(nameof(AuditableEntity.UpdatedBy));
                entity.Property<byte[]>(nameof(AuditableEntity.RowVersion))
                    .IsRowVersion()
                    .IsConcurrencyToken();
            }

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                ConfigureSoftDelete(modelBuilder, entityType.ClrType);
            }
        }

        return modelBuilder;
    }

    private static void ConfigureSoftDelete(ModelBuilder modelBuilder, Type entityType)
    {
        var entity = modelBuilder.Entity(entityType);
        entity.Property<bool>(nameof(ISoftDelete.IsDeleted)).HasDefaultValue(false).IsRequired();
        entity.Property<DateTimeOffset?>(nameof(ISoftDelete.DeletedAtUtc)).HasPrecision(0);
        entity.Property<Guid?>(nameof(ISoftDelete.DeletedBy));
        entity.HasIndex(nameof(ISoftDelete.IsDeleted));

        var parameter = Expression.Parameter(entityType, "entity");
        var isDeleted = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var predicate = Expression.Lambda(Expression.Equal(isDeleted, Expression.Constant(false)), parameter);
        entity.HasQueryFilter(predicate);
    }
}
