using LearningPortal.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>Defines the SQL Server schema mapping for courses.</summary>
public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(course => course.Id);
        builder.Property(course => course.Title).HasMaxLength(200).IsRequired();
        builder.Property(course => course.Description).HasMaxLength(4_000).IsRequired();
        builder.HasIndex(course => course.Title);
    }
}
