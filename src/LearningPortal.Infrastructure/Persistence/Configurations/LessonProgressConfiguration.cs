#pragma warning disable CS1591
using LearningPortal.Domain.Learning;
using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

/// <summary>Defines persistence and uniqueness rules for learner progress.</summary>
public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    internal const string EnrollmentLessonIndexName = "UX_LessonProgress_EnrollmentId_LessonId";
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ToTable("LessonProgress"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<LearningPortal.Domain.Enrollments.Enrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LearningPortal.Domain.Lessons.Lesson>().WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EnrollmentId, x.LessonId }).IsUnique().HasDatabaseName(EnrollmentLessonIndexName);
        builder.HasIndex(x => x.StudentId); builder.HasIndex(x => x.Status); builder.HasIndex(x => x.LastAccessedAtUtc);
    }
}
