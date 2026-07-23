using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>Defines SQL Server enrollment mapping and constraints.</summary>
public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    internal const string ActiveEnrollmentIndexName = "UX_Enrollments_CourseId_StudentId_Active";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CourseId, x.StudentId })
            .IsUnique().HasDatabaseName(ActiveEnrollmentIndexName)
            .HasFilter("[Status] <> N'Withdrawn'");
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.EnrolledAtUtc);
    }
}
