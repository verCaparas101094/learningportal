using LearningPortal.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>Defines SQL Server course mapping.</summary>
public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    internal const string SlugUniqueIndexName = "IX_Courses_Slug";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(course => course.Id);

        builder.Property(course => course.Title).HasMaxLength(200).IsRequired();
        builder.Property(course => course.Slug).HasMaxLength(200).IsRequired();
        builder.Property(course => course.Description).HasMaxLength(5_000).IsRequired();
        builder.Property(course => course.Category).HasMaxLength(100).IsRequired();
        builder.Property(course => course.ThumbnailUrl).HasMaxLength(2_048);
        builder.Property(course => course.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(course => course.InstructorId).IsRequired();
        builder.Property(course => course.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(course => course.Slug)
            .HasDatabaseName(SlugUniqueIndexName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(course => course.Status);
        builder.HasIndex(course => course.InstructorId);
        builder.HasIndex(course => course.Category);
        builder.HasIndex(course => course.CreatedAtUtc);
    }
}
