#pragma warning disable CS1591
using LearningPortal.Domain.Skills;
using LearningPortal.Domain.Quizzes;
using LearningPortal.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningPortal.Infrastructure.Persistence.Configurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(skill => skill.Id);
        builder.Property(skill => skill.Name).HasMaxLength(100).IsRequired();
        builder.Property(skill => skill.Slug).HasMaxLength(100).IsRequired();
        builder.Property(skill => skill.Description).HasMaxLength(1000);
        builder.Property(skill => skill.RowVersion).IsRowVersion();
        builder.HasIndex(skill => skill.Slug).IsUnique();
        builder.HasIndex(skill => new { skill.IsActive, skill.Name });
    }
}

public sealed class InstructorEligibilityConfiguration : IEntityTypeConfiguration<InstructorEligibility>
{
    public void Configure(EntityTypeBuilder<InstructorEligibility> builder)
    {
        builder.ToTable("InstructorEligibility");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.BestPercentage).HasPrecision(5, 2);
        builder.Property(value => value.RowVersion).IsRowVersion();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Skill>().WithMany().HasForeignKey(value => value.SkillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Quiz>().WithMany().HasForeignKey(value => value.QualifyingQuizId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => new { value.UserId, value.SkillId }).IsUnique();
        builder.HasIndex(value => new { value.SkillId, value.IsEligible });
    }
}
