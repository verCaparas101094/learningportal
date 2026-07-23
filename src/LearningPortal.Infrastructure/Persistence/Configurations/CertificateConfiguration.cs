#pragma warning disable CS1591
using LearningPortal.Domain.Certificates;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates"); builder.HasKey(value => value.Id);
        builder.Property(value => value.CertificateNumber).HasMaxLength(40).IsRequired();
        builder.Property(value => value.VerificationCode).HasMaxLength(80).IsRequired();
        builder.Property(value => value.StudentDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(value => value.CourseTitle).HasMaxLength(200).IsRequired();
        builder.Property(value => value.CourseCategory).HasMaxLength(100).IsRequired();
        builder.Property(value => value.InstructorDisplayName).HasMaxLength(200);
        builder.Property(value => value.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.RevocationReason).HasMaxLength(1000);
        builder.Property(value => value.RowVersion).IsRowVersion();
        builder.HasOne<Enrollment>().WithMany().HasForeignKey(value => value.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Course>().WithMany().HasForeignKey(value => value.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.CertificateNumber).IsUnique();
        builder.HasIndex(value => value.VerificationCode).IsUnique();
        builder.HasIndex(value => value.EnrollmentId).IsUnique();
        builder.HasIndex(value => value.StudentId);
        builder.HasIndex(value => value.CourseId);
        builder.HasIndex(value => value.IssuedAtUtc);
        builder.HasIndex(value => value.Status);
    }
}
