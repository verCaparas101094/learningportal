using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>Defines SQL Server lesson mapping.</summary>
public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    internal const string CourseOrderIndexName = "IX_Lessons_CourseId_Order";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(100_000).IsRequired();
        builder.Property(x => x.LessonType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CourseId, x.Order }).HasDatabaseName(CourseOrderIndexName)
            .IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.CourseId, x.Status });
        builder.HasIndex(x => x.Title);
    }
}
